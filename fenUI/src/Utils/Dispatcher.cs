namespace FenUISharp
{
    public class Dispatcher
    {
        private ulong _currentTick = 0;
        private List<DispatcherCall> dispatcherCalls = new();

        internal void UpdateQueue()
        {
            _currentTick++;

            dispatcherCalls.ToList().ForEach(x =>
            {
                switch (x.mode)
                {
                    case 0:
                        if (_currentTick - x.tickAtCall >= x.ticksLater)
                        {
                            x.action.Invoke();
                            dispatcherCalls.Remove(x);
                        }
                        break;
                    case 1:
                        if (DateTime.Now.Subtract(x.timeAtCall).TotalSeconds >= x.sLater)
                        {
                            x.action.Invoke();
                            dispatcherCalls.Remove(x);
                        }
                        break;
                    case 2:
                        x.action.Invoke();
                        dispatcherCalls.Remove(x);
                        break;
                }
            });
        }

        public void Invoke(Action action)
        {
            dispatcherCalls.Add(new(action));
        }

        public void InvokeLater(Action action, ulong ticks)
        {
            dispatcherCalls.Add(new(action, _currentTick, ticks));
        }

        public void InvokeLater(Action action, float seconds)
        {
            dispatcherCalls.Add(new(action, seconds));
        }
    }

    internal class DispatcherCall
    {
        public Action action;
        public DateTime timeAtCall;

        public float sLater = 0;

        public ulong tickAtCall = 0;
        public ulong ticksLater = 0;

        public int mode = 0;

        public DispatcherCall(Action action, ulong tickAtCall, ulong ticksLater)
        {
            this.action = action;
            this.tickAtCall = tickAtCall;
            this.ticksLater = ticksLater;
        }

        public DispatcherCall(Action action, float sLater)
        {
            this.action = action;
            this.timeAtCall = DateTime.Now;
            this.sLater = sLater;

            mode = 1;
        }
        
        public DispatcherCall(Action action)
        {
            this.action = action;

            mode = 2;
        }
    }
}