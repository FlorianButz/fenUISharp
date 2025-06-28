using System.Security.Cryptography;
using SkiaSharp;

namespace FenUISharp.Materials
{
    /// <summary>
    /// A material is a way to re-use the same look across multiple UIObjects, without having duplicate code. It also allows for quick style swapping
    /// </summary>
    public abstract class Material
    {
        private Dictionary<string, object?> _props = new();

        public T GetProp<T>(string key, T defaultValue) => _props.TryGetValue(key, out var value) ? (value != null ? (T)value : defaultValue) : defaultValue;
        public void SetProp(string key, object? value) => _props[key] = value;

        public Material WithOverride(Dictionary<string, object> overrides)
        {
            Material clone = (Material)this.MemberwiseClone();

            // Make sure the dict does not point to the original one 
            clone._props = new Dictionary<string, object?>(_props);

            foreach (var kvp in overrides)
                clone.SetProp(kvp.Key, kvp.Value);
            return clone;
        }

        protected SKPaint GetDefaultPaint() => new() { IsAntialias = true, IsDither = true };

        public void DrawWithMaterial(SKCanvas targetCanvas, SKRect rect, SKPaint? paint = null)
        {
            using var tempPath = new SKPath();
            tempPath.AddRect(rect);
            DrawWithMaterial(targetCanvas, tempPath, paint);
        }

        public void DrawWithMaterial(SKCanvas targetCanvas, SKRoundRect rect, SKPaint? paint = null)
        {
            using var tempPath = new SKPath();
            tempPath.AddRoundRect(rect);
            DrawWithMaterial(targetCanvas, tempPath, paint);
        }

        public void DrawWithMaterial(SKCanvas targetCanvas, SKPoint position, float radius, SKPaint? paint = null)
        {
            using var tempPath = new SKPath();
            tempPath.AddCircle(position.X, position.Y, radius);
            DrawWithMaterial(targetCanvas, tempPath, paint);
        }

        public void DrawWithMaterial(SKCanvas targetCanvas, SKPath path, SKPaint? paint = null)
        {
            using var tempPath = new SKPath(path);
            using var tempPaint = paint?.Clone() ?? GetDefaultPaint();
            Draw(targetCanvas, tempPath, tempPaint);
        }

        protected abstract void Draw(SKCanvas targetCanvas, SKPath path, SKPaint paint);

        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (!(obj is Material)) return false;

            Material objMat = (Material)obj;
            if (!objMat._props.SequenceEqual(_props)) return false;
            if (objMat.GetType().Name != GetType().Name) return false;

            return true;
        }
    }
}