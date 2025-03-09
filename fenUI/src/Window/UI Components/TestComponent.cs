using DynamicWin.Utils;
using SkiaSharp;

namespace FenUISharp
{
    public class TestComponent : UIComponent
    {
        public TestComponent(float x, float y, float width, float height) : base(x, y, width, height)
        {
            // useSurfaceCaching = false;
            transform.boundsPadding.SetValue(this, 50, 15);

            renderQuality.SetValue(this, .9f, 25);
        
            scaleOrder = new SecondOrder(transform.size, 4f, 2f);
        }

        SecondOrder scaleOrder;

        public Vector2 overrideSize = new Vector2(100, 100);

        protected override void OnUpdate()
        {
            base.OnUpdate();

            var s = transform.size;

            transform.size = scaleOrder.Update(FWindow.DeltaTime, overrideSize);
            
            renderQuality.SetValue(this, s.Equals(transform.size) ? 1 : 0.75f, 25);
            
            if(!s.Equals(transform.size))
                Invalidate();
        }

        protected override void OnMouseEnter()
        {
            base.OnMouseEnter();
            overrideSize = new Vector2(300, 200);
        }

        protected override void OnMouseExit()
        {
            base.OnMouseEnter();
            overrideSize = new Vector2(100, 100);
        }

        // protected override void OnSelectedLost()
        // {
        //     base.OnSelectedLost();
        //     skPaint.Color = SKColors.Pink;
        //     Invalidate();
        // }

        // protected override void OnSelected()
        // {
        //     base.OnSelected();
            
        //     skPaint.Color = SKColors.Blue;
        //     Invalidate();
        // }

        protected override void DrawToSurface(SKCanvas canvas)
        {
            // canvas.Clear(SKColors.Aqua);
            canvas.DrawRoundRect(transform.localBounds, 15, 15, skPaint);
        }
    }
}