using FenUISharp.Components.Text.Layout;
using FenUISharp.Components.Text.Model;
using FenUISharp.Components.Text.Rendering;
using FenUISharp.Mathematics;
using SkiaSharp;

namespace FenUISharp.Components.Text
{
    public class FText : UIComponent
    {
        protected TextModel _model;
        public TextModel Model { get { return _model; } set { _model = value; OnModelChanged?.Invoke(); OnAnyChange?.Invoke(); Invalidate(); } }

        protected TextRenderer _renderer;
        public TextRenderer Renderer { get { return _renderer; } set { _renderer = value; OnRendererChanged?.Invoke(); OnAnyChange?.Invoke(); Invalidate(); } }

        protected TextLayout _layout;
        public TextLayout Layout { get { return _layout; } set { _layout = value; OnLayoutChanged?.Invoke(); OnAnyChange?.Invoke(); Invalidate(); } }

        public Action? OnModelChanged { get; set; }
        public Action? OnRendererChanged { get; set; }
        public Action? OnLayoutChanged { get; set; }

        public Action? OnAnyChange { get; set; }

        public FText(Window rootWindow, Vector2 position, Vector2 size, TextModel model) : base(rootWindow, position, size)
        {
            _model = model;
            _renderer = new(this);
            _layout = new WrapLayout(this);

            PixelSnapping = false;
            Transform.BoundsPadding.SetValue(this, 25, 25);
        }

        protected override void DrawToSurface(SKCanvas canvas)
        {
            List<Glyph> glyphs = _layout.ProcessModel(_model, Transform.LocalBounds);
            _renderer.DrawText(canvas, _model, glyphs, SkPaint);
        }
    }
}