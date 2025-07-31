using FenUISharp.Objects;

namespace FenUISharp.States
{
    /// <summary>
    /// A State is a generic priority aware responsive value type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class State<T> : IDisposable
    {
        public WeakReference<UIObject> Owner { get; init; }
        public bool ManualResolve { get; set; } = false;

        public Func<T> Value { private get => GetValue(); set => SetResponsiveState(value); }
        public T CachedValue { get => _lastValue; }

        private List<StateEntry<T>> values = new();
        private Func<List<StateEntry<T>>, StateEntry<T>> _resolver;
        private Func<T, T> _processor;

        private T _lastValue;

        private List<IStateListener> _listener = new();
        private List<Action<T>> _action = new();

        /// <summary>
        /// Will ignore the default value if the set is not empty. Useful to avoid interference with custom resolvers
        /// </summary>
        public bool IgnoreFirstValueIfSetNotEmpty { get; set; } = true;

        public State(Func<T> defaultValue, UIObject owner, Action<T>? action = null, bool manualResolve = false)
        {
            Owner = new(owner);
            if (action != null) Subscribe(action);

            // Add initial value
            values.Add(new() { Value = defaultValue, Priority = 0 });
            this._lastValue = defaultValue();
            this._resolver = entries => entries.OrderBy(x => x.Priority).Last();
            this._processor = value => value;

            this.ManualResolve = manualResolve;

            if (FContext.GetCurrentWindow() == null)
                throw new Exception("States can only be declared in a valid FenUISharp window context");
            else
                FContext.GetCurrentWindow().OnPreUpdate += Update;

            owner.OnObjectDisposed += Dispose;
        }

        public State(Func<T> defaultValue, UIObject owner, IStateListener? listener = null, bool manualResolve = false)
        {
            Owner = new(owner);
            if (listener != null) Subscribe(listener);

            // Add initial value
            values.Add(new() { Value = defaultValue, Priority = 0 });
            this._lastValue = defaultValue();
            this._resolver = entries => entries.OrderBy(x => x.Priority).Last();
            this._processor = value => value;

            this.ManualResolve = manualResolve;

            // I guess this is stupid since it requires removing the state in a dispose method, meaning every state would need to be disposed
            if (FContext.GetCurrentWindow() == null)
                throw new Exception("States can only be declared in a valid FenUISharp window context");
            else
                FContext.GetCurrentWindow().OnPreUpdate += Update;

            owner.OnObjectDisposed += Dispose;
        }

        private Func<T> GetValue()
        {
            if (values.Count == 0) throw new InvalidOperationException("No values available");
            return _resolver(values).Value;
        }

        /// <summary>
        /// A resolver can modify the order in which entries are prioritized. By default, the highest priority is the active value
        /// </summary>
        /// <param name="resolver"></param>
        public void SetResolver(Func<List<StateEntry<T>>, StateEntry<T>> resolver)
        {
            _resolver = resolver;
            UpdateList();
        }

        /// <summary>
        /// A processor can modify the returned value. This can be used for clamping
        /// </summary>
        /// <param name="processor"></param>
        public void SetProcessor(Func<T, T> processor)
        {
            _processor = processor;
        }

        public void SetStaticState(T value, uint priority = 0)
        {
            // Always add 1 to priority, so the default value does not get overriden
            if (priority != uint.MaxValue)
                priority++;

            // Replace if priority already exists
            if (values.Any(x => x.Priority == priority)) values.RemoveAll(x => x.Priority == priority);
            values.Add(new() { Value = () => value, Priority = priority, IsStatic = true });

            UpdateList();
        }

        public void SetResponsiveState(Func<T> value, uint priority = 0)
        {
            if (priority != uint.MaxValue)
                priority++;

            // Replace if priority already exists
            if (values.Any(x => x.Priority == priority)) values.RemoveAll(x => x.Priority == priority);
            values.Add(new() { Value = value, Priority = priority });

            UpdateList();
        }

        public void DissolvePriority(uint priority)
        {
            if (values.Any(x => x.Priority == priority)) values.RemoveAll(x => x.Priority == priority);
        }

        public void UpdateList()
        {
            if (values.Count == 0) return;

            var valuesMod = new List<StateEntry<T>>(values);
            if (IgnoreFirstValueIfSetNotEmpty && values.Count > 1) valuesMod.RemoveAt(0);

            var winningEntry = _resolver(valuesMod);

            var last = _lastValue;
            var value = winningEntry.Value();

            _lastValue = _processor(value);

            if (!EqualityComparer<T>.Default.Equals(last, _lastValue))
                FContext.GetCurrentDispatcher().Invoke(() => Notify(_lastValue));
        }


        private void Update()
        {
            if (!ManualResolve)
                ReevaluateValue();
        }

        public void ReevaluateValue(bool forceReevaluation = false)
        {
            if (Value == null) return;
            var value = _processor(Value());

            var lastVal = _lastValue;
            _lastValue = value;
            if (!EqualityComparer<T>.Default.Equals(lastVal, value) || forceReevaluation) Notify(value);
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

            if (Owner.TryGetTarget(out var target))
                target.OnObjectDisposed -= Dispose;

            values = new();
            _resolver = entries => entries.OrderBy(x => x.Priority).Last();
            _listener = new();
            _action = new();
        }
    }

    /// <summary>
    /// Adds often used resolvers for the State class
    /// </summary>
    public static class StateResolverTemplates
    {
        public static Func<List<StateEntry<float>>, StateEntry<float>> BiggestFloatResolver =>
            entries => entries.OrderBy(e => e.Value()).Last();

        public static Func<List<StateEntry<int>>, StateEntry<int>> BiggestIntResolver =>
            entries => entries.OrderBy(e => e.Value()).Last();

        public static Func<List<StateEntry<float>>, StateEntry<float>> SmallestFloatResolver =>
            entries => entries.OrderByDescending(e => e.Value()).Last();

        public static Func<List<StateEntry<int>>, StateEntry<int>> SmallestIntResolver =>
            entries => entries.OrderByDescending(e => e.Value()).Last();
    }

    public struct StateEntry<T>
    {
        public bool IsStatic;
        public Func<T> Value;
        public uint Priority;
    }
}