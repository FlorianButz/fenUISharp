using FenUISharp.Mathematics;
using FenUISharp.Objects;
using FenUISharp.States;
using SkiaSharp;

namespace FenUISharp.Objects
{
    public class Compositor : IDisposable, IStateListener
    {
        public UIObject Owner { get; init; }

        public State<int> LocalZIndex { get; init; }
        public int CreationIndex { get; init; }

        [ThreadStatic]
        private static int GlobalCreationIndex = 0;

        public Compositor(UIObject owner)
        {
            this.Owner = owner;

            CreationIndex = GlobalCreationIndex;
            GlobalCreationIndex++;

            LocalZIndex = new(() => 0, this);
        }

        public List<UIObject> GetZOrderedListOfChildren(UIObject root)
        {
            if (root == null) return new();
            
            return root.Children
                .OrderBy(child => child.Composition.LocalZIndex.CachedValue)
                .ThenBy(child => child.Composition.CreationIndex)
                .ToList();
        }

        public List<UIObject> GetZOrderedListOfEverything(UIObject root)
        {
            if (root == null) return new();

            var result = new List<UIObject>();
            TraverseAndCollect(root, result);
            return result;
        }

        void TraverseAndCollect(UIObject current, List<UIObject> list) {
            list.Add(current);

            var sortedChildren = current.Children
                .OrderBy(child => child.Composition.LocalZIndex.CachedValue)
                .ThenBy(child => child.Composition.CreationIndex)
                .ToList();

            foreach (var child in sortedChildren) {
                TraverseAndCollect(child, list);
            }
        }

        public bool TestIfTopMost()
        {
            // Check if the last element matches the current one
            var last = GetZOrderedListOfEverything(FContext.GetRootViewPane()).LastOrDefault(x => x != null);

            // If it's a match, this object is the topmost
            return last != null && last == Owner;
        }

        public SKImage? GrabBehindPlusBuffer(SKRect globalBounds, float quality)
        {
            SKImage? behind = FContext.GetCurrentWindow().RenderContext.CaptureWindowRegion(globalBounds, quality);
            SKImage? buffer = Owner.Parent?._childSurface.CaptureSurfaceRegion(Owner.Parent.Transform.GlobalToDrawLocal(globalBounds), quality);

            Compositor.Dump(behind, "buffer_grab_behind");
            Compositor.Dump(buffer, "buffer_grab_child");

            if (behind == null) return buffer;
            else if (buffer == null) return behind;

            SKImage combined = RMath.Combine(behind, buffer, new(filter: SKFilterMode.Linear, mipmap: SKMipmapMode.Linear));

            Compositor.Dump(buffer, "buffer_grab_combined");

            behind.Dispose();
            buffer.Dispose();

            return combined;
        }

        public static void Dump(SKImage? image, string name)
        {
            // COMMENT THIS OUT FOR DEBUG DUMPS. WARNING: THIS WILL DUMP EVERY TIME THIS METHOD GETS CALLED EVERY FRAME, DON'T RE-RENDER TOO OFTEN
            return;

            if (image == null) return;

            string dir = Path.Combine(AppContext.BaseDirectory, "Dumps");
            if(!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            using (var data = image.Encode(SKEncodedImageFormat.Png, 80))
            using (var stream = File.OpenWrite(Path.Combine(dir, $"{name}_dump{DateTime.Now.Ticks}.png")))
            {
                // save the data to a stream
                data.SaveTo(stream);
            }
        }

        public void Dispose()
        {
            LocalZIndex.Dispose();
        }

        public void OnInternalStateChanged<T>(T value)
        {
            Owner.Invalidate(UIObject.Invalidation.TransformDirty);
        }
    }
}