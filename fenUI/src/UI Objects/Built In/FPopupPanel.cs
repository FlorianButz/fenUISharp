using FenUISharp.Behavior;
using FenUISharp.Mathematics;
using FenUISharp.States;
using FenUISharp.WinFeatures;
using SkiaSharp;

namespace FenUISharp.Objects
{
    public class FPopupPanel : FPanel, IStateListener
    {
        public float TailHeight { get; set; } = 10;
        public float TailWidth { get; set; } = 7.5f;
        public float TailCornerRadius { get; set; } = 3.5f;

        public int DistanceToTarget { get; set; } = 15;

        public bool DisposeOnClose { get; set; } = false;

        public State<Vector2> GlobalTargetPoint { get; init; }
        public SKRect GlobalBounds { get; set; }

        private AnimatorComponent _inAnimation;
        private StackContentComponent? layout;

        public FPopupPanel(Func<Vector2> size, bool addLayout = true) : base(() => new(0, 0), size)
        {
            GlobalTargetPoint = new(() => new(0, 0), this);

            if (addLayout)
            {
                layout = new StackContentComponent(this, StackContentComponent.ContentStackType.Vertical, StackContentComponent.ContentStackBehavior.Scroll);
                layout.Pad.SetStaticState(5);
                layout.Gap.SetStaticState(15);

                layout.ContentFade = true;
                layout.EnableEdgeBlur = true;
                layout.FadeLength = 20;
            }

            _inAnimation = new(this, Easing.EaseOutLessElastic, Easing.EaseInCubic);
            _inAnimation.OnValueUpdate += (x) =>
            {
                Transform.Scale.SetStaticState(Vector2.One * x);
            };
            _inAnimation.OnComplete += () => OnCompleteAnim();

            Visible.SetStaticState(false);
            Enabled.SetStaticState(false);
            InteractiveSurface.IgnoreInteractions.SetStaticState(true);
            InteractiveSurface.IgnoreChildInteractions.SetStaticState(true);

            LayoutObject layoutObject = new(this);
            layoutObject.IgnoreParentLayout.SetStaticState(true);
            CornerRadius.SetStaticState(10f);

            Composition.LocalZIndex.SetResponsiveState(() => IsShowing ? 99 : 0);
            dispatcher = FContext.GetCurrentDispatcher();

            WindowFeatures.GlobalHooks.OnMouseAction += OnGlobalMouseAction;
        }

        private Dispatcher dispatcher;
        private void OnGlobalMouseAction(MouseInputCode code)
        {
            dispatcher.Invoke(() =>
            {
                if (code.button != MouseInputButton.Left || code.state != MouseInputState.Down) return;
                if (_inAnimation.IsRunning) return;    // Don't during show/close anims

                if (!Shape.GlobalBounds.Contains(new SKPoint(FContext.GetCurrentWindow().ClientMousePosition.x, FContext.GetCurrentWindow().ClientMousePosition.y)))
                    Close(); // Close this pop-up
            });
        }

        public bool IsShowing { get; private set; }

        public void ToggleShow(Func<Vector2>? targetPoint = null)
        {
            if (IsShowing) Close();
            else Show(targetPoint);
        }

        public void Show(Func<Vector2>? targetPoint = null)
        {
            if (_inAnimation.IsRunning) return;
            if (IsShowing) return;

            Enabled.SetStaticState(true);
            FContext.GetCurrentDispatcher().InvokeLater(() =>
            {
                InteractiveSurface.IgnoreInteractions.SetStaticState(false);
                InteractiveSurface.IgnoreChildInteractions.SetStaticState(false);
                Visible.SetStaticState(true);
            }, 5L);
            if (targetPoint != null)
                GlobalTargetPoint.SetResponsiveState(targetPoint);

            IsShowing = true; // Set this only once, here
            _inAnimation.OnComplete = () =>
            {
                // Remove the duplicate IsShowing = true
                OnCompleteAnim();
            };
            _inAnimation.Duration = 0.75f;
            _inAnimation.Inverse = false;
            _inAnimation.Restart();
            OnStartAnim();
        }

        public void Close(Action? onComplete = null)
        {
            // Add a flag to track if we're in the process of closing
            if (!IsShowing && !_inAnimation.IsRunning)
            {
                onComplete?.Invoke();
                return;
            }

            if (_inAnimation.IsRunning)
            {
                _inAnimation.OnComplete += () => Close(onComplete);
                return;
            }

            IsShowing = false;
            _inAnimation.Inverse = true;
            _inAnimation.Duration = 0.175f;
            _inAnimation.OnComplete = () =>
            {
                InteractiveSurface.IgnoreInteractions.SetStaticState(true);
                InteractiveSurface.IgnoreChildInteractions.SetStaticState(true);
                FContext.GetCurrentDispatcher().InvokeLater(() =>
                {
                    Visible.SetStaticState(false);
                    Enabled.SetStaticState(false);
                }, 1L);
                onComplete?.Invoke();
                OnCompleteAnim();
                if (DisposeOnClose) Dispose();
            };
            _inAnimation.Restart();
        }

        void OnCompleteAnim()
        {
            Children.ToList().ForEach(x => x.Invalidate(Invalidation.All));
        }

        void OnStartAnim()
        {
            FContext.GetCurrentDispatcher().InvokeLater(() => Children.ToList().ForEach(x => x.Invalidate(Invalidation.All)), 1L);
            _inAnimation.OnComplete = OnCompleteAnim; // Reset

            layout?.FullUpdateLayout();
        }

        private SKPath tailPath;
        private SKPath tailClip;
        private Vector2 lastPos = new();

        protected override void LateUpdate()
        {
            base.LateUpdate();

            GlobalBounds = FContext.GetCurrentWindow().Bounds;
            Transform.LocalPosition.SetStaticState(GetPanelPosition(Transform.GlobalToLocal(GlobalTargetPoint.CachedValue), Transform.GlobalToLocal(GlobalBounds), DistanceToTarget));

            // Calc
            GetTailSpecifics(out Vector2 basePoint, out float degrees);

            tailPath = GetTailPath(new(basePoint.x, basePoint.y), degrees);
            tailClip = GetTailClip(new(basePoint.x, basePoint.y), degrees);

            if (lastPos != Transform.LocalToGlobal(Transform.Position))
            {
                layout?.FullUpdateLayout();
                Children.ForEach(x => x.Invalidate(Invalidation.TransformDirty));
            }

            lastPos = Transform.LocalToGlobal(Transform.Position);
        }

        public Vector2 LocalToAnchor(Vector2 localPos)
        {
            float anchorX = (localPos.x - Shape.SurfaceDrawRect.Left) / Shape.SurfaceDrawRect.Width;
            float anchorY = (localPos.y - Shape.SurfaceDrawRect.Top) / Shape.SurfaceDrawRect.Height;
            return new Vector2(anchorX, anchorY);
        }

        void GetTailSpecifics(out Vector2 basePoint, out float degrees)
        {
            Vector2 point = Transform.GlobalToDrawLocal(GlobalTargetPoint.CachedValue);

            // Right area
            if (point.x > Shape.LocalBounds.Right)
            {
                basePoint = new(Shape.LocalBounds.Right, RMath.Clamp(point.y, Shape.LocalBounds.Top + CornerRadius.CachedValue + 5, Shape.LocalBounds.Bottom - CornerRadius.CachedValue - 5));
                degrees = -90;

                Transform.Anchor.SetStaticState(LocalToAnchor(new(basePoint.x + 50, basePoint.y)));
                return;
            }

            // Left area
            if (point.x < Shape.LocalBounds.Left)
            {
                basePoint = new(Shape.LocalBounds.Left, RMath.Clamp(point.y, Shape.LocalBounds.Top + CornerRadius.CachedValue + 5, Shape.LocalBounds.Bottom - CornerRadius.CachedValue - 5));
                degrees = 90;

                Transform.Anchor.SetStaticState(LocalToAnchor(new(basePoint.x - 50, basePoint.y)));
                return;
            }

            // Top area
            if (point.y < Shape.LocalBounds.Top)
            {
                basePoint = new(RMath.Clamp(point.x, Shape.LocalBounds.Left + CornerRadius.CachedValue + 5, Shape.LocalBounds.Right - CornerRadius.CachedValue - 5), Shape.LocalBounds.Top);
                degrees = 180;

                Transform.Anchor.SetStaticState(LocalToAnchor(new(basePoint.x, basePoint.y - 50)));
                return;
            }

            // Default and bottom
            basePoint = new(Shape.LocalBounds.MidX, Shape.LocalBounds.Bottom);
            degrees = 0;
            Transform.Anchor.SetStaticState(LocalToAnchor(new(basePoint.x, basePoint.y + 50)));
        }

        Vector2 GetPanelPosition(Vector2 point, SKRect bounds, int distanceToTarget, int pad = 15)
        {
            Vector2 ret = point;

            Vector2 offset = new(0, -Shape.SurfaceDrawRect.Height / 2 - distanceToTarget);

            // Calculate dynamic positioning to find best spot

            // Top edge
            if ((point.y - Shape.LocalBounds.Height / 2 - pad + offset.y) < bounds.Top)
            {
                offset.y = Shape.SurfaceDrawRect.Height / 2 + distanceToTarget;
            }
            // Bottom edge
            if ((point.y + Shape.LocalBounds.Height / 2 + pad) > bounds.Right)
            {
                offset.y = -Shape.SurfaceDrawRect.Height / 2 - distanceToTarget;
            }

            // Right edge
            if ((point.x + Shape.LocalBounds.Width / 2 + pad) > bounds.Right)
            {
                offset.x = -Shape.SurfaceDrawRect.Width / 2 - distanceToTarget;
                offset.y = 0;
            }
            // Left edge
            if ((point.x - Shape.LocalBounds.Width / 2 - pad) < bounds.Left)
            {
                offset.x = Shape.SurfaceDrawRect.Width / 2 + distanceToTarget;
                offset.y = 0;
            }

            // Apply offset of dynamic positioning
            ret += offset;

            // Make sure to clamp to safe area
            ret = Vector2.Clamp(ret,
                new(bounds.Left + pad + Shape.LocalBounds.Width / 2,
                    bounds.Top + pad + Shape.LocalBounds.Height / 2),
                new(
                    bounds.Right - pad - Shape.LocalBounds.Width / 2,
                    bounds.Bottom - pad - Shape.LocalBounds.Height / 2)
            );

            return ret;
        }

        public override void Render(SKCanvas canvas)
        {
            var paint = GetRenderPaint();

            RenderMaterial.CachedValue.DrawWithMaterial(canvas, tailPath, this, paint);

            // Clip only tail area
            canvas.ClipPath(this.tailPath, SKClipOperation.Difference, true);
            canvas.ClipPath(tailClip, SKClipOperation.Difference, true);

            // Draw base everywhere except tail
            base.Render(canvas);
        }

        public SKPath GetTailClip(SKPoint middlePoint, float rotationDegrees = 0f)
        {
            return GetTailPath(middlePoint, rotationDegrees, isClip: true);
        }

        public SKPath GetTailPath(SKPoint middlePoint, float rotationDegrees = 0f, bool isClip = false)
        {
            var path = new SKPath();

            // Define the three main points
            SKPoint p1 = new(middlePoint.X - TailWidth, middlePoint.Y);
            SKPoint p2 = new(middlePoint.X, middlePoint.Y + TailHeight);
            SKPoint p3 = new(middlePoint.X + TailWidth, middlePoint.Y);

            SKPoint before_p1 = new(p1.X - TailCornerRadius, p1.Y);
            SKPoint after_p1 = new(
                p1.X + TailCornerRadius * (p2.X - p1.X) / (float)Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2)),
                p1.Y + TailCornerRadius * (p2.Y - p1.Y) / (float)Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2))
            );

            float len1to2 = (float)Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
            float len2to3 = (float)Math.Sqrt(Math.Pow(p3.X - p2.X, 2) + Math.Pow(p3.Y - p2.Y, 2));

            SKPoint before_p2 = new(
                p2.X - TailCornerRadius * (p2.X - p1.X) / len1to2,
                p2.Y - TailCornerRadius * (p2.Y - p1.Y) / len1to2
            );
            SKPoint after_p2 = new(
                p2.X + TailCornerRadius * (p3.X - p2.X) / len2to3,
                p2.Y + TailCornerRadius * (p3.Y - p2.Y) / len2to3
            );

            SKPoint before_p3 = new(
                p3.X - TailCornerRadius * (p3.X - p2.X) / len2to3,
                p3.Y - TailCornerRadius * (p3.Y - p2.Y) / len2to3
            );
            SKPoint after_p3 = new(p3.X + TailCornerRadius, p3.Y); // going outward

            // Build the path with rounded corners using QuadTo
            path.MoveTo(before_p1);
            path.QuadTo(p1, after_p1);
            path.LineTo(before_p2);
            path.QuadTo(p2, after_p2);
            path.LineTo(before_p3);
            path.QuadTo(p3, after_p3);

            if (isClip)
            {
                path.Reset();
                path.AddRect(
                    new(Shape.SurfaceDrawRect.MidX - TailWidth - 0 - TailCornerRadius,
                    Shape.LocalBounds.Bottom,
                    Shape.SurfaceDrawRect.MidX + TailWidth + 0 + TailCornerRadius,
                    Shape.LocalBounds.Bottom + 1.5f)
                );
            }

            if (rotationDegrees != 0f)
            {
                var matrix = SKMatrix.CreateIdentity();

                matrix = SKMatrix.Concat(matrix, SKMatrix.CreateTranslation(middlePoint.X, middlePoint.Y));
                matrix = SKMatrix.Concat(matrix, SKMatrix.CreateRotationDegrees(rotationDegrees));
                matrix = SKMatrix.Concat(matrix, SKMatrix.CreateTranslation(-middlePoint.X, -middlePoint.Y));

                path.Transform(matrix);
            }

            return path;
        }

        public override void Dispose()
        {
            base.Dispose();

            GlobalTargetPoint.Dispose();
            WindowFeatures.GlobalHooks.OnMouseAction -= OnGlobalMouseAction;
        }
    }
}