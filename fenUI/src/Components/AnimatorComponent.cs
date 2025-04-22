using System.Diagnostics;
using FenUISharp.Components;

namespace FenUISharp
{

    public class AnimatorComponent : Component
    {
        public float Duration { get; set; } = 1;
        
        public float Time { get {
                if (Duration <= 0f) return 1f;
                return Math.Clamp(_timePassed / Duration, 0f, 1f);
            }
        }

        private float _timePassed = 0;
        public Action<float>? onValueUpdate;
        public Action? onComplete;

        public bool IsRunning { get; private set; } = false;
        private Func<float, float> easing;
        private Func<float, float> inverseEasing;

        public bool Inverse { get; set; } = false;
        public bool AutoLowerRenderQuality { get; set; } = false;

        private float startValue;
        private float targetValue;
        private float currentValue;

        public AnimatorComponent(UIComponent parent, Func<float, float> easing, Func<float, float>? inverseEasing = null) : base(parent)
        {
            this.easing = easing;
            if(inverseEasing == null) this.inverseEasing = easing;
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

        public override void ComponentUpdate()
        {
            base.ComponentUpdate();

            if (AutoLowerRenderQuality)
            {
                if (IsRunning) Parent.RenderQuality.SetValue(this, 0.9f, 50);
                else Parent.RenderQuality.DissolveValue(this);
            }
            else Parent.RenderQuality.DissolveValue(this);

            if (!IsRunning)
                return;

            _timePassed += (float)Parent.WindowRoot.DeltaTime;

            // Normalize time and clamp between 0 and 1.
            float t = Math.Clamp(_timePassed / Duration, 0f, 1f);
            float easedT = (Inverse) ? inverseEasing(t) : easing(t);

            // Interpolate from the starting value to the target value.
            currentValue = startValue + (targetValue - startValue) * easedT;
            onValueUpdate?.Invoke(currentValue);
            Parent.SoftInvalidate();

            if (_timePassed >= Duration)
            {
                IsRunning = false;
                onComplete?.Invoke();
            }
        }

        public override void ComponentDestroy()
        {
            base.ComponentDestroy();
            onValueUpdate = null;
            onComplete = null;
        }
    }

}