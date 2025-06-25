namespace FenUISharp.States
{
    public class State<T> : IDisposable
    {
        private Func<T>? _value;
        private Func<T> _defaultValue;

        public Func<T> Value { private get => (_value == null) ? _defaultValue : _value; set => SetResponsiveState(value); }
        public T CachedValue { get => _lastValue ?? _defaultValue(); }
        private T? _lastValue;

        private List<IStateListener> _listener = new();
        private List<Action<T>> _action = new();

        public bool ManualResolve { get; set; } = false;

        private bool isStaticValue = false;

        public void SetStaticState(T value)
        {
            if (!EqualityComparer<T>.Default.Equals(_lastValue, value))
            {
                _lastValue = value;

                // Make sure to notify at the start of update
                FContext.GetCurrentDispatcher().Invoke(() => Notify(value));
            }

            isStaticValue = true;
        }

        public void SetResponsiveState(Func<T> value)
        {
            var v = value();
            if (!EqualityComparer<T>.Default.Equals(_lastValue, v))
            {
                _lastValue = v;
                _value = value;

                // Make sure to notify at the start of update
                FContext.GetCurrentDispatcher().Invoke(() => Notify(v));
            }

            isStaticValue = false;
        }

        public State(Func<T> defaultValue, IStateListener? listener = null, bool manualResolve = false)
        {
            if (listener != null) Subscribe(listener);
            this._defaultValue = defaultValue;

            this.ManualResolve = manualResolve;
            this._lastValue = defaultValue();

            // I guess this is stupid since it requires removing the state in a dispose method, meaning every state would need to be disposed
            if (FContext.GetCurrentWindow() == null)
                throw new Exception("States can only be declared in a valid FenUISharp window context");
            else
                FContext.GetCurrentWindow().OnPreUpdate += Update;
        }

        private void Update()
        {
            if (!ManualResolve)
                ReevaluateValue();
        }

        public State(Func<T> defaultValue, Action<T>? action = null, bool manualResolve = false)
        {
            if (action != null) Subscribe(action);
            this._defaultValue = defaultValue;

            this.ManualResolve = manualResolve;
            this._lastValue = defaultValue();

            if (FContext.GetCurrentWindow() == null)
                throw new Exception("States can only be declared in a valid FenUISharp window context");
            else
                FContext.GetCurrentWindow().OnPreUpdate += Update;
        }

        public void ReevaluateValue()
        {
            if (Value == null || isStaticValue) return;
            var value = Value();

            if (_lastValue == null) Notify(value);
            else if (!EqualityComparer<T>.Default.Equals(_lastValue, value)) Notify(value);

            _lastValue = value;
        }

        private void Notify(T value)
        {
            _listener.ToList().ForEach(x => x.OnInternalStateChanged<T>(value));
            _action.ToList().ForEach(x => x?.Invoke(value));
        }

        public void Subscribe(IStateListener listener) => _listener.Add(listener);
        public void Unsubscribe(IStateListener listener) => _listener.Remove(listener);

        public void Subscribe(Action<T> action) => _action.Add(action);
        public void Unsubscribe(Action<T> action) => _action.Remove(action);

        public void Dispose()
        {
            if (FContext.GetCurrentWindow() != null)
                FContext.GetCurrentWindow().OnPreUpdate -= Update;
        }
    }
}