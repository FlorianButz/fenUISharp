using FenUISharp.Mathematics;
using FenUISharp.Objects;
using FenUISharp.States;
using SkiaSharp;

namespace FenUISharp.Objects
{
    public class Compositor : IDisposable, IStateListener
    {
        public WeakReference<UIObject> Owner { get; init; }

        public State<int> LocalZIndex { get; init; }
        public int CreationIndex { get; init; }

        [ThreadStatic]
        private static int GlobalCreationIndex = 0;

        // Static stuff
        [ThreadStatic]
        private static int activeInstances = 0;

        [ThreadStatic]
        private static List<UIObject> _cachedOrderedList = new();

        public static bool EnableDump { get; set; } = false;

        public Compositor(UIObject owner)
        {
            this.Owner = new(owner);

            CreationIndex = GlobalCreationIndex;
            GlobalCreationIndex++;

            LocalZIndex = new(() => 0, owner, this);

            if (activeInstances == 0)
                FContext.GetCurrentWindow().Callbacks.OnPreUpdate += CacheZOrderedListOfEverything;

            activeInstances++;
        }

        private static void CacheZOrderedListOfEverything()
        {
            if (FContext.GetRootViewPane() != null)
            {
                var result = new List<UIObject>();
                TraverseAndCollect(FContext.GetRootViewPane(), result, false);
                _cachedOrderedList = result;
            }
        }

        public List<UIObject> GetZOrderedListOfChildren(UIObject root)
        {
            return root.Children
                .OrderBy(child => child.Composition.LocalZIndex.CachedValue)
                .ThenBy(child => child.Composition.CreationIndex)
                .ToList();
        }

        public List<UIObject> GetZOrderedListOfEnabled()
        {
            return _cachedOrderedList.Where(x => x.GlobalEnabled && x.GlobalVisible).ToList();
        }

        public List<UIObject> GetZOrderedListOfEverything()
        {
            return _cachedOrderedList;
        }

        static void TraverseAndCollect(UIObject current, List<UIObject> list, bool enabledAndVisibleOnly = false)
        {
            if (enabledAndVisibleOnly ? (!current.GlobalVisible || !current.GlobalEnabled) : false) return;
            list.Add(current);

            var sortedChildren = current.Children
                .Where(x => (enabledAndVisibleOnly ? (x.GlobalEnabled && x.GlobalVisible) : true))
                .OrderBy(child => child.Composition.LocalZIndex.CachedValue)
                .ThenBy(child => child.Composition.CreationIndex)
                .ToList();

            foreach (var child in sortedChildren)
            {
                TraverseAndCollect(child, list, enabledAndVisibleOnly);
            }
        }

        public bool TestIfTopMost()
        {
            // Check if the last element matches the current one
            var last = GetZOrderedListOfEnabled().LastOrDefault(x => x != null);


            // If it's a match, this object is the topmost
            if (Owner.TryGetTarget(out var owner))
                return last != null && last == owner;
            return false;
        }

        public SKImage? GrabBehindPlusBuffer(SKRect globalBounds, float quality)
        {
            if (globalBounds.Height < 1 || globalBounds.Width < 1) return null;

            SKImage? behind = FContext.GetCurrentWindow().SkiaDirectCompositionContext?.CaptureWindowRegion(globalBounds, quality);

            return behind;

            // Not needed anymore since the switch to not using a child cached combined surface
            // SKImage? buffer = Owner.Parent?._childSurface.CaptureSurfaceRegion(Owner.Parent.Transform.GlobalToDrawLocal(globalBounds), quality);

            // Compositor.Dump(behind, "buffer_grab_behind");
            // Compositor.Dump(buffer, "buffer_grab_child");

            // if (behind == null || !RMath.IsImageValid(behind)) return buffer;
            // else if (buffer == null || !RMath.IsImageValid(buffer)) return behind;

            // SKImage? combined = RMath.Combine(behind, buffer, new(filter: SKFilterMode.Linear, mipmap: SKMipmapMode.Linear)) ?? null;

            // Compositor.Dump(buffer, "buffer_grab_combined");

            // behind.Dispose();
            // buffer.Dispose();

            // return combined;
        }

        public static void Dump(SKImage? image, string name)
        {
            if (!EnableDump) return;
            if (image == null) return;

            string dir = Path.Combine(AppContext.BaseDirectory, "Dumps");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            using (var data = image.Encode(SKEncodedImageFormat.Png, 80))
            using (var stream = File.OpenWrite(Path.Combine(dir, $"{name}_dump{DateTime.Now.Ticks}.png")))
            {
                // save the data to a stream
                data.SaveTo(stream);
            }
        }

        public void Dispose()
        {
            activeInstances--;
            if (activeInstances <= 0)
                FContext.GetCurrentWindow().Callbacks.OnPreUpdate -= CacheZOrderedListOfEverything;
        }

        public void OnInternalStateChanged<T>(T value)
        {
            if (Owner.TryGetTarget(out var owner))
                owner.Invalidate(UIObject.Invalidation.TransformDirty);
        }
    }
}