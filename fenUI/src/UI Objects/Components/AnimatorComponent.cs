using System.Diagnostics;
using FenUISharp.Objects;

namespace FenUISharp.Behavior
{
    public class AnimatorComponent : BehaviorComponent
    {
        public float Duration { get; set; } = 1;

        public float Time
        {
            get
            {
                if (Duration <= 0f) return 1f;
                float t = Math.Clamp(_timePassed / Duration, 0f, 1f);
                float easedT = (Inverse) ? inverseEasing(t) : easing(t);
                return easedT;
            }
        }

        public float UneasedTime
        {
            get
            {
                if (Duration <= 0f) return 1f;
                float t = Math.Clamp(_timePassed / Duration, 0f, 1f);
                return t;
            }
        }

        private float _timePassed = 0;
        public Action<float>? OnValueUpdate { get; set; }
        public Action? OnComplete { get; set; }

        public bool IsRunning { get; private set; } = false;
        private Func<float, float> easing;
        private Func<float, float> inverseEasing;

        public bool Inverse { get; set; } = false;

        private float startValue;
        private float targetValue;
        private float currentValue;

        public AnimatorComponent(UIObject owner, Func<float, float> easing, Func<float, float>? inverseEasing = null) : base(owner)
        {
            this.easing = easing;
            if (inverseEasing == null) this.inverseEasing = easing;
            else this.inverseEasing = inverseEasing;

            // Initialize current value based on the expected default.
            currentValue = !Inverse ? 0f : 1f;
            startValue = currentValue;
            targetValue = Inverse ? 0f : 1f;
        }

        public void Start()
        {
            // If already running, update the start value to the current value.
            // Otherwise, if idle, use the last known current value.
            startValue = currentValue;
            // The target remains the same: 1 when not inverse, 0 when inverse.
            targetValue = Inverse ? 0f : 1f;
            _timePassed = 0;
            IsRunning = true;
        }

        public void Restart()
        {
            startValue = Inverse ? 1f : 0f;
            targetValue = Inverse ? 0f : 1f;
            _timePassed = 0;
            IsRunning = true;
        }

        public void Break()
        {
            _timePassed = 0;
            IsRunning = false;
        }

        public override void HandleEvent(BehaviorEventType type, object? data = null)
        {
            base.HandleEvent(type, data);

            if (type == BehaviorEventType.BeforeUpdate)
            {
                if (!IsRunning)
                    return;

                _timePassed += (float)FContext.GetCurrentWindow().DeltaTime;

                // Normalize time and clamp between 0 and 1.
                float t = Math.Clamp(_timePassed / Duration, 0f, 1f);
                float easedT = (Inverse) ? inverseEasing(t) : easing(t);

                // Interpolate from the starting value to the target value.
                currentValue = startValue + (targetValue - startValue) * easedT;
                OnValueUpdate?.Invoke(currentValue);

                if (_timePassed >= Duration)
                {
                    IsRunning = false;
                    OnComplete?.Invoke();
                }
            }
        }

        public override void ComponentDestroy()
        {
            base.ComponentDestroy();
            OnValueUpdate = null;
            OnComplete = null;
        }
    }
}