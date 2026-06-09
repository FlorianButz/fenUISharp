using System.Diagnostics;
using FenUISharp.Logging;
using FenUISharp.Mathematics;
using FenUISharp.Native;

namespace FenUISharp
{
    public class FWindowLoop : IDisposable
    {
        private WeakReference<FWindow> window { get; set; }
        public FWindow Window { get => window.TryGetTarget(out var target) ? target : throw new Exception("Window not set."); }

        public bool PauseUpdateLoopWhenLoseFocus { get; set; } = false;
        public bool PauseUpdateLoopWhenHidden { get; set; } = true;

        public bool ReduceRefreshrateWhenInactive { get; set; } = true;
        public int ReducedRefreshrateWhenInactive { get; set; } = 30;

        public bool CapFrameTime { get; set; } = false;
        public float MaxFrameTime { get; set; } = 32;

        private bool _delayedFocus = true;
        private bool _lastFrameClickable = true;
        private Vector2 _cursorPosLastFrame;

        internal Func<bool>? _logicIsRunning { get; set; }
        internal Func<bool>? _windowIsRunning { get; set; }
        private Thread? LogicThread { get; set; }

        private volatile bool _shutdownRequested;

        public FWindowLoop(FWindow window)
        {
            this.window = new WeakReference<FWindow>(window);
        }

        public void Begin(Action? setupLogic)
        {
            Thread.CurrentThread.Name = "Window Thread";

            // Creating a new thread for the logic loop
            // This includes logic updates and rendering
            LogicThread = new Thread(() =>
            {
                setupLogic?.Invoke();
                SetupLogic();

                LoopLogic();
            });

            LogicThread.Name = "Logic Thread";
            LogicThread.IsBackground = false;
            LogicThread.Priority = ThreadPriority.Lowest;
            LogicThread.SetApartmentState(ApartmentState.STA); // Very important for DC and DX
            LogicThread.Start();

            // Setting up the Windows message loop for the window
            MSG msg;
            while (true)
            {
                // Dispatch queued events (must be called before checking _isRunning
                // so that DestroyWindow scheduled by Dispose() is executed even
                // when _isRunning was set to false during message dispatch)
                Window?.WindowDispatcher?.UpdateQueue();

                // Check if someone messaged me
                while (Win32APIs.PeekMessage(out msg, IntPtr.Zero, 0, 0, 1))
                {
                    // Translate and send messages
                    Win32APIs.TranslateMessage(ref msg);
                    Win32APIs.DispatchMessage(ref msg);
                }

                // Exit condition: only exit when this window is done AND no other windows remain
                if (!(_windowIsRunning?.Invoke() ?? false))
                {
                    if (FenUI.activeInstances.Count == 0)
                        break;
                    // Other windows still active — keep pumping messages for them
                }

                // Shorter sleep for better message responsiveness
                Thread.Sleep((Window?.Properties?.IsWindowFocused ?? false) ? 2 : 8);
            }
        }

        internal void InterruptThread()
        {
            _shutdownRequested = true;

            if (LogicThread != null && LogicThread.IsAlive)
            {
                // Check if being called from the logic thread itself.
                if (LogicThread.ManagedThreadId == Environment.CurrentManagedThreadId)
                {
                    FLogger.Log<FWindowLoop>("InterruptThread called from LogicThread — skipping self-join.");
                    return;
                }

                if (!LogicThread.Join(500))
                {
                    FLogger.Log<FWindowLoop>("Logic thread did not exit within 500ms. It will exit on next loop iteration.");
                }
            }
        }

        protected virtual void SetupLogic()
        {

        }

        protected virtual void LoopLogic()
        {
            // Creating stopwatch for frame timing
            Stopwatch stopwatch = Stopwatch.StartNew();

            double nextFrameTime = 0;
            double previousFrameTime = 0;

            while ((_logicIsRunning?.Invoke() ?? false) && !_shutdownRequested)
            {
                bool isFocused = Window.Properties.IsWindowFocused;
                bool isVisible = Window.Properties.IsWindowVisible;

                // Calculating frame interval based on target refresh rate
                double frameInterval = 1000.0 / 
                    (ReduceRefreshrateWhenInactive 
                        ? ((!isVisible || !isFocused) ? ReducedRefreshrateWhenInactive : Window.TargetRefreshRate) 
                        : Window.TargetRefreshRate);

                // Getting the current time and calculating the time until the next frame
                double currentTime = stopwatch.Elapsed.TotalMilliseconds;
                double timeUntilNextFrame = nextFrameTime - currentTime;

                if (!_delayedFocus && PauseUpdateLoopWhenLoseFocus || !isVisible && PauseUpdateLoopWhenHidden)
                {
                    // Make sure to update delayed focus
                    _delayedFocus = isFocused;

                    // Make sure delta time doesn't go too crazy when updates are resumed
                    previousFrameTime = currentTime;

                    WindowUpdate(true);

                    // Exit out of the loop if the flag got changed
                    if (!_logicIsRunning.Invoke()) return;

                    // Sleep longer if unfocused or minimized
                    Thread.Sleep(!Window.Properties.IsWindowVisible ? 1000 : 250); continue;
                }

                if (timeUntilNextFrame <= 0)
                {
                    // Increment frame count for caching
                    FContext.CurrentFrameCount++;

                    // Calculate delta time and set the previous frame time
                    Window.Time.DeltaTime = (float)(currentTime - previousFrameTime) / 1000.0f;
                    previousFrameTime = currentTime;

                    FContext.frameStartTime = DateTime.Now;

                    // Calling the window update
                    WindowUpdate();

                    // Exit out of the loop if the flag got changed
                    if (!_logicIsRunning.Invoke()) return;

                    if (Window.Surface.IsNextFrameRendering())
                    {
                        Window.Callbacks.OnBeginRender?.Invoke();

                        // Executing the draw action
                        Window.RenderResources?.Draw();

                        Window.Callbacks.OnEndRender?.Invoke();

                        // Reset flags
                        Window._isDirty = false;
                        Window._fullRedraw = false;
                    }

                    // Call end frame after drawing
                    Window.Surface.RootViewPane?.OnEndFrame();

                    // Calculate time until next frame
                    nextFrameTime = currentTime + frameInterval;

                    // Setting the delayed focus variable
                    _delayedFocus = Window.Properties.IsWindowFocused;
                }
                else
                {
                    // Smarter sleep to reduce CPU usage while maintaining responsiveness
                    // Only sleep if we have meaningful time until next frame
                    if (timeUntilNextFrame > 1.0)
                    {
                        // Use shorter sleep when close to next frame, longer when waiting longer
                        int sleepTime = timeUntilNextFrame > 8.0 ? 1 : 0;
                        if (sleepTime > 0)
                            Thread.Sleep(sleepTime);
                    }
                    // Spin wait for very short durations (more responsive than sleep)
                    else if (timeUntilNextFrame > 0.1)
                    {
                        Thread.SpinWait(100);
                    }
                }
            }
        }

        protected virtual void WindowUpdate(bool isPaused = false)
        {
            // Add delta time to current time
            Window.Time.Time += Window.Time.DeltaTime;

            Window.CallUpdate();
            Window.Shape.UpdateShape();

            // Execute queued logic events
            Window.LogicDispatcher.UpdateQueue(); // This MUST be executed as first in update

            if (!(_logicIsRunning?.Invoke() ?? true)) return;

            // Call the pre update iteration, only if not paused
            if (!isPaused)
            {
                // Area hit test
                bool areaClickable = Window.IsAreaClickable(Window.ClientMousePosition);
                if (_lastFrameClickable != areaClickable)
                {
                    _lastFrameClickable = areaClickable;
                    Window.Properties.ToggleClickability(areaClickable);
                }

                // Mouse position update
                Window.ClientMousePosition = Win32APIs.GetClientCursorPosition(Window.hWnd);
                if (_cursorPosLastFrame != Window.ClientMousePosition)
                {
                    Window.Callbacks.OnMouseMove?.Invoke(Window.ClientMousePosition);
                    _cursorPosLastFrame = Window.ClientMousePosition;
                }

                Window.Callbacks.OnPreUpdate?.Invoke();

                // Reverse update iteration, only if not paused
                // Get the tree of all ui objects
                var tree = Window.Surface.RootViewPane?.Composition.GetZOrderedListOfEverything();
                if (tree != null)
                {
                    for (int i = tree.Count - 1; i >= 0; i--)
                        tree[i].OnReverseUpdate();
                }

                // Normal update iteration, only if not paused
                Window.Surface.RootViewPane?.OnEarlyUpdate();
                Window.Surface.RootViewPane?.OnUpdate(); // First update iteration

                // Post update iteration, only if not paused
                Window.Callbacks.OnPostUpdate?.Invoke();

                // Second reverse update, iterate in reverse again
                if (tree != null)
                {
                    for (int i = tree.Count - 1; i >= 0; i--)
                        tree[i].OnLateReverseUpdate();
                }

                // Late update iteration, only if not paused
                Window.Surface.RootViewPane?.OnLateUpdate(); // Second update iteration
            }
        }

        public void Dispose()
        {
            FLogger.Log<FWindowLoop>("Disposing window loop and logic thread");

            if (LogicThread != null)
            {
                if (LogicThread.IsAlive)
                {
                    _shutdownRequested = true;
                    if (!LogicThread.Join(500))
                    {
                        try { LogicThread.Interrupt(); } catch { }
                    }
                }
                LogicThread = null;
            }
        }
    }
}