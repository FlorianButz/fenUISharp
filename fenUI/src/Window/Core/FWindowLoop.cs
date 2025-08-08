using System.Diagnostics;
using FenUISharp.Mathematics;
using FenUISharp.Native;

namespace FenUISharp
{
    public class FWindowLoop : IDisposable
    {
        private WeakReference<FWindow> window { get; set; }
        public FWindow Window { get => window.TryGetTarget(out var target) ? target : throw new Exception("Window not set."); }

        public Func<bool>? _isRunning { get; set; }

        private Thread? LogicThread { get; set; }

        public FWindowLoop(FWindow window)
        {
            this.window = new WeakReference<FWindow>(window);
        }

        public void Begin(Action? setupLogic)
        {
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
            LogicThread.SetApartmentState(ApartmentState.STA); // Very important for DC and DX
            LogicThread.Start();

            // Setting up the Windows message loop for the window

            MSG msg;
            while (_isRunning?.Invoke() ?? false)
            {
                while (Win32APIs.GetMessage(out msg, IntPtr.Zero, 0, 0))
                {
                    Win32APIs.TranslateMessage(ref msg);
                    Win32APIs.DispatchMessage(ref msg);
                    Thread.Sleep(1); // Prevent too high cpu usage
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

            // Calculating frame interval based on target refresh rate
            double frameInterval = 1000.0 / Window.TargetRefreshRate;
            double nextFrameTime = 0;
            double previousFrameTime = 0;

            while (_isRunning?.Invoke() ?? false)
            {
                // Getting the current time and calculating the time until the next frame
                double currentTime = stopwatch.Elapsed.TotalMilliseconds;
                double timeUntilNextFrame = nextFrameTime - currentTime;

                if (timeUntilNextFrame <= 0)
                {
                    Window.Time.DeltaTime = (float)(currentTime - previousFrameTime) / 1000.0f;
                    previousFrameTime = currentTime;

                    // Executing the draw action
                    Window.SkiaDirectCompositionContext?.Draw();

                    nextFrameTime = currentTime + frameInterval;
                }
                else if (timeUntilNextFrame > 2.0)
                    Thread.Sleep(1);
                else
                    Thread.SpinWait(20);
            }
        }

        public void Dispose()
        {
            this.LogicThread?.Interrupt();
            this.LogicThread = null;
        }
    }
}