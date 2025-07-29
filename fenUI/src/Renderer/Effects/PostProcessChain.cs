using FenUISharp.Objects;
using SkiaSharp;

namespace FenUISharp.RuntimeEffects
{
    public class PostProcessChain : IDisposable
    {
        private WeakReference<UIObject>? WeakOwner;
        public UIObject? Owner
        {
            get
            {
                if (WeakOwner?.TryGetTarget(out var target) ?? false) return target;
                return null; // Usually shouldn't happen but some edge cases may lead to it
            }
        }

        public PostProcessChain(UIObject owner) =>
            this.WeakOwner = new(owner);

        private List<IPostProcessEffect> _effects { get; set; } = new();

        public void Attach(IPostProcessEffect effect) => _effects.Add(effect);
        public void Detatch(IPostProcessEffect effect) => _effects.Remove(effect);

        public void OnBeforeRender(PPInfo info)
        {
            if (Owner == null) return;
            info.owner = Owner;

            _effects.ToList().ForEach(x => x.OnBeforeRender(info));
        }

        public void OnAfterRender(PPInfo info)
        {
            if (Owner == null) return;
            info.owner = Owner;
            
            _effects.ToList().ForEach(x => x.OnAfterRender(info));
        }

        public void OnLateAfterRender(PPInfo info)
        {
            if (Owner == null) return;
            info.owner = Owner;
            
            _effects.ToList().ForEach(x => x.OnLateAfterRender(info));
        }

        public void Dispose()
        {
            _effects.ForEach(x =>
            {
                if (x is IDisposable) ((IDisposable)x).Dispose();
            });
            _effects = new();

            WeakOwner = null;
        }
    }
}