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

        public bool CapFrameTime { get; set; } = false;
        public float MaxFrameTime { get; set; } = 32;

        private bool _delayedFocus = true;
        private bool _lastFrameClickable = true;
        private Vector2 _cursorPosLastFrame;

        public Func<bool>? _logicIsRunning { get; set; }
        public Func<bool>? _windowIsRunning { get; set; }
        private Thread? LogicThread { get; set; }

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
            LogicThread.Priority = ThreadPriority.Highest;
            LogicThread.SetApartmentState(ApartmentState.STA); // Very important for DC and DX
            LogicThread.Start();

            // Setting up the Windows message loop for the window
            MSG msg;
            while (_windowIsRunning?.Invoke() ?? false) // Loop should only run as long as window is alive
            {
                // Dispatch queued events
                Window?.WindowDispatcher?.UpdateQueue();

                // Check if someone messaged me
                while (Win32APIs.PeekMessage(out msg, IntPtr.Zero, 0, 0, 1))
                {
                    // Translate and send messages
                    Win32APIs.TranslateMessage(ref msg);
                    Win32APIs.DispatchMessage(ref msg);
                }

                // Shorter sleep for better message responsiveness (reduced from 15ms to 8ms)
                Thread.Sleep((Window?.Properties?.IsWindowFocused ?? false) ? 2 : 8);
            }
        }

        protected virtual void SetupLogic()
        {

        }

        protected virtual void LoopLogic()
        {
            // Creating stopwatch for frame timing
            Stopwatch stopwatch = Stopwatch.StartNew();

            // Calculating frame interval based on target refresh rate
            double frameInterval = 1000.0 / Window.TargetRefreshRate;
            double nextFrameTime = 0;
            double previousFrameTime = 0;

            while (_logicIsRunning?.Invoke() ?? false)
            {
                // Getting the current time and calculating the time until the next frame
                double currentTime = stopwatch.Elapsed.TotalMilliseconds;
                double timeUntilNextFrame = nextFrameTime - currentTime;

                if (!_delayedFocus && PauseUpdateLoopWhenLoseFocus || !Window.Properties.IsWindowVisible && PauseUpdateLoopWhenHidden)
                {
                    // Make sure to update delayed focus
                    _delayedFocus = Window.Properties.IsWindowFocused;

                    // Make sure delta time doesn't go too crazy when updates are resumed
                    previousFrameTime = currentTime;

                    WindowUpdate(true);

                    // Exit out of the loop if the flag got changed
                    if (!_logicIsRunning.Invoke()) return;

                    // Sleep longer if unfocused or minimized
                    Thread.Sleep(!Window.Properties.IsWindowVisible ? 300 : 100); continue;
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
                        Window.SkiaDirectCompositionContext?.Draw();

                        Window.Callbacks.OnEndRender?.Invoke();

                        // Reset flags
                        Window._isDirty = false;
                        Window._fullRedraw = false;
                    }

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
                        Thread.SpinWait(10);
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
                if(_lastFrameClickable != areaClickable)
                {
                    _lastFrameClickable = areaClickable;
                    Window.Properties.ToggleClickability(areaClickable);
                }

                // Mouse position update
                Window.ClientMousePosition = Win32APIs.GetClientCursorPosition(Window.hWnd);
                if(_cursorPosLastFrame != Window.ClientMousePosition)
                {
                    Window.Callbacks.OnMouseMove?.Invoke(Window.ClientMousePosition);
                    _cursorPosLastFrame = Window.ClientMousePosition;   
                }

                Window.Callbacks.OnPreUpdate?.Invoke();

                // Reverse update iteration, only if not paused
                // Get the tree of all ui objects
                var tree = Window.Surface.RootViewPane?.Composition.GetZOrderedListOfEverything().ToList();
                // Reverse and execute in order
                tree?.Reverse(); tree?.ForEach(x => { x.OnReverseUpdate(); });

                // Normal update iteration, only if not paused
                Window.Surface.RootViewPane?.OnEarlyUpdate();
                Window.Surface.RootViewPane?.OnUpdate(); // First update iteration

                // Post update iteration, only if not paused
                Window.Callbacks.OnPostUpdate?.Invoke();

                // Second reverse update, re-use reversed tree from first iteration
                tree?.ForEach(x => { x.OnLateReverseUpdate(); });

                // Late update iteration, only if not paused
                Window.Surface.RootViewPane?.OnLateUpdate(); // Second update iteration

                Window.Surface.RootViewPane?.OnEndFrame(); // End frame
            }
        }

        public void Dispose()
        {
            FLogger.Log<FWindowLoop>("Disposing window loop and logic thread");

            // Destroy and dispose logic thread
            this.LogicThread?.Interrupt();
            this.LogicThread = null;
        }
    }
}