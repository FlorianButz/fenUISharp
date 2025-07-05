using FenUISharp.Mathematics;
using FenUISharp.Objects.Text.Layout;
using FenUISharp.Objects.Text.Model;
using FenUISharp.Objects.Text.Rendering;
using SkiaSharp;

namespace FenUISharp.Objects.Text
{
    public class FText : UIObject
    {
        protected TextModel _model;
        public TextModel Model { get { return _model; } set { var lastModel = _model; _model = value; if(lastModel != _model) { OnModelChanged?.Invoke(); OnAnyChange?.Invoke(); Invalidate(Invalidation.All); } } }

        protected TextRenderer _renderer;
        public TextRenderer Renderer { get { return _renderer; } set { _renderer = value; OnRendererChanged?.Invoke(); OnAnyChange?.Invoke(); Invalidate(Invalidation.SurfaceDirty); } }

        protected TextLayout _layout;
        public TextLayout LayoutModel { get { return _layout; } set { _layout = value; OnLayoutChanged?.Invoke(); OnAnyChange?.Invoke(); Invalidate(Invalidation.All); } }

        public Action? OnModelChanged { get; set; }
        public Action? OnRendererChanged { get; set; }
        public Action? OnLayoutChanged { get; set; }

        public Action? OnAnyChange { get; set; }

        public FText(TextModel model, Func<Vector2>? position = null, Func<Vector2>? size = null) : base(position, size)
        {
            _model = model;
            _renderer = new(this);
            _layout = new WrapLayout(this);

            Padding.SetStaticState(10);
        }

        public void SilentSetModel(TextModel model)
        {
            var lastModel = this._model;
            this._model = model;

            if (lastModel.TextParts != this._model.TextParts) Invalidate(Invalidation.All);
        }

        public override void Render(SKCanvas canvas)
        {
            List<Glyph> glyphs = _layout.ProcessModel(_model, Shape.LocalBounds);

            using var paint = GetRenderPaint();
            _renderer.DrawText(canvas, _model, glyphs, Shape.SurfaceDrawRect, paint);
        }
    }
}