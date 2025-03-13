using SkiaSharp;

namespace FenUISharp
{
    public class TestComponent : FUIComponent
    {

        AnimatorComponent anim;

        public TestComponent(Vector2 position, Vector2 size) : base(position, size)
        {
            // useSurfaceCaching = false;
            transform.boundsPadding.SetValue(this, 50, 15);

            renderQuality.SetValue(this, .9f, 25);
        
            // scaleOrder = new FSecondOrder(transform.scale, 1.5f, 0.6f);

            //transform.matrix = transform.Create3DRotationMatrix(0, 0, 0, 500);

            FWindowsMediaControls.onThumbnailUpdated += Invalidate;
        
            anim = new AnimatorComponent(this, FEasing.EaseOutQuint);
            anim.duration = 0.5f;

            anim.onValueUpdate += (t) => {
                transform.scale = FMath.Lerp(new Vector2(1, 1), new Vector2(2, 2), t);
            };

            components.Add(anim);
        }

        // FSecondOrder scaleOrder;

        // public Vector2 overrideSize = new Vector2(1, 1);

// float t =0;
//         protected override void OnUpdate()
//         {
//             base.OnUpdate();
// t+= 5f;
//             // transform.matrix = transform.Create3DRotationMatrix(t / 3, t, t * 2);

//             var s = transform.scale;
//             transform.scale = scaleOrder.Update(FWindow.DeltaTime, overrideSize);
//             renderQuality.SetValue(this, s.Equals(transform.scale) ? 1 : 0.9f, 25);
            
// // transform.rotation += 0.5f;

//             if(!s.Equals(transform.scale))
//                 Invalidate();
//         }

        protected override void OnMouseEnter()
        {
            base.OnMouseEnter();

            anim.inverse = false;
            anim.Start();
        }

        protected override void OnMouseExit()
        {
            base.OnMouseEnter();
            
            anim.inverse = true;
            anim.Start();
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

        protected override void OnMouseDown()
        {
            base.OnMouseDown();

            // WindowsMediaControls.TriggerMediaControl(MediaControlTrigger.SwapLoopMode);
            FWindowsMediaControls.TriggerMediaControl(MediaControlTrigger.ToggleShuffle);
        }

        protected override void DrawToSurface(SKCanvas canvas)
        {
            canvas.Clear(SKColors.Aqua.WithAlpha(25));
            //canvas.DrawRoundRect(transform.localBounds, 15, 15, skPaint);
            
            if(FWindowsMediaControls.CachedInfo.isActiveSession && FWindowsMediaControls.CachedInfo.thumbnail != null)
                canvas.DrawImage(FWindowsMediaControls.CachedInfo.thumbnail, transform.localBounds, skPaint);
        }
    }
}