using SkiaSharp;

namespace FenUISharp.Materials
{
    public class MaterialCompose : Material
    {
        public Func<Material> BottomMaterial { get; init; }
        public Func<Material> TopMaterial { get; init; }

        public MaterialCompose(Func<Material> bottomMaterial, Func<Material> topMaterial)
        {
            BottomMaterial = bottomMaterial;
            TopMaterial = topMaterial;
        }

        protected override void Draw(SKCanvas targetCanvas, SKPath path, SKPaint paint)
        {
            BottomMaterial().DrawWithMaterial(targetCanvas, path, paint);
            TopMaterial().DrawWithMaterial(targetCanvas, path, paint);
        }
    }
}