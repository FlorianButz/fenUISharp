using System.Diagnostics.CodeAnalysis;

namespace FenUISharp
{
    public class MultiAccess<T>
    {
        public T Value { get => GetValue(); }

        public Action<T>? onValueUpdated;

        public struct MA_V
        {
            public object caller;
            public T value;
            public float priority;
        }

        List<MA_V> valueList = new List<MA_V>();

        public MultiAccess(T defaultValue = default(T))
        {
            valueList.Add(new MA_V { caller = this, priority = 0, value = defaultValue });
        }

        public void SetValue(object caller, T value, float priority)
        {
            if (valueList.Any(x => x.caller == caller && x.value != null && x.value.Equals(value))) return;

            var valueBefore = Value;

            if (valueList.Any(x => x.caller == caller))
            {
                DissolveValue(caller);
            }

            for (int i = 0; i < valueList.Count; i++)
            {
                if (priority > valueList[i].priority)
                {
                    valueList.Insert(i, new MA_V { caller = caller, priority = priority, value = value });

                    if(!EqualityComparer<T>.Default.Equals(Value, valueBefore))
                        onValueUpdated?.Invoke(GetValue());
                    return;
                }
            }

            valueList.Insert(valueList.Count, new MA_V { caller = caller, priority = priority, value = value });

            if(!EqualityComparer<T>.Default.Equals(Value, valueBefore))
                onValueUpdated?.Invoke(GetValue());
        }

        public void DissolveValue(object caller)
        {
            var valueBefore = Value;

            int index = -1;
            for (int i = 0; i < valueList.Count; i++)
            {
                if (valueList[i].caller == caller)
                    index = i;
            }

            if (index != -1)
                valueList.RemoveAt(index);

            if(!EqualityComparer<T>.Default.Equals(Value, valueBefore))
                onValueUpdated?.Invoke(GetValue());
        }

        T GetValue()
        {
            return valueList[0].value;
        }
    }
}