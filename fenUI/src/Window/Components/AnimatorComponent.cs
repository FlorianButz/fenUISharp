using System.Diagnostics;

namespace FenUISharp
{

    public class AnimatorComponent : FComponent
    {
        public float duration { get; set; } = 1;

        private float _timePassed = 0;
        public Action<float>? onValueUpdate;
        public Action? onComplete;

        private bool isRunning { get; set; } = false;
        private Func<float, float> easing;

        public bool inverse { get; set; } = false;
        public bool autoLowerRenderQuality { get; set; } = false;

        private float startValue;
        private float targetValue;
        private float currentValue;

        public AnimatorComponent(FUIComponent parent, Func<float, float> easing) : base(parent)
        {
            this.easing = easing;
            // Initialize current value based on the expected default.
            currentValue = !inverse ? 0f : 1f;
            startValue = currentValue;
            targetValue = inverse ? 0f : 1f;
        }

        public void Start()
        {
            // If already running, update the start value to the current value.
            // Otherwise, if idle, use the last known current value.
            startValue = currentValue;
            // The target remains the same: 1 when not inverse, 0 when inverse.
            targetValue = inverse ? 0f : 1f;
            _timePassed = 0;
            isRunning = true;
        }

        public override void OnComponentUpdate()
        {
            base.OnComponentUpdate();

            if (autoLowerRenderQuality)
            {
                if (isRunning) parent.renderQuality.SetValue(this, 0.9f, 50);
                else parent.renderQuality.DissolveValue(this);
            }
            else parent.renderQuality.DissolveValue(this);

            if (!isRunning)
                return;

            _timePassed += FWindow.DeltaTime;

            // Normalize time and clamp between 0 and 1.
            float t = Math.Clamp(_timePassed / duration, 0f, 1f);
            float easedT = easing(t);

            // Interpolate from the starting value to the target value.
            currentValue = startValue + (targetValue - startValue) * easedT;
            onValueUpdate?.Invoke(currentValue);

            if (_timePassed >= duration)
            {
                isRunning = false;
                onComplete?.Invoke();
            }
        }
    }

}