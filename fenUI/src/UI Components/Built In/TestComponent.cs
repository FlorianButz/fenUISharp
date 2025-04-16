// using FenUISharp.Components;
// using FenUISharp.Mathematics;
// using SkiaSharp;

// namespace FenUISharp
// {
//     public class TestComponent : UIComponent
//     {
//         public TestComponent(Window rootWindow, Vector2 position, Vector2 size) : base(rootWindow, position, size)
//         {
//         }

//         float x1 = 0, x2 = 0;
//         float t = 0;

//         protected override void OnUpdate()
//         {
//             base.OnUpdate();
//             t += 1f * (float)WindowRoot.DeltaTime;

//             x1 = (float)Math.Sin(t) * 100 + 50;
//             x2 = (float)Math.Sin(t + 0.5f) * 100 + 50;

//             Invalidate();
//         }

//         protected override void DrawToSurface(SKCanvas canvas)
//         {
//             int layer = canvas.SaveLayer();

//             var info = new SKImageInfo((int)Transform.LocalBounds.Width, (int)Transform.LocalBounds.Height);

//             // Configure common paint settings
//             var paint = new SKPaint
//             {
//                 IsAntialias = true,
//                 Style = SKPaintStyle.Fill,
//                 // Adjust the alpha channel for transparency if needed
//                 Color = new SKColor(0, 122, 255, 200) // For example a semi-transparent blue
//             };

//             // Define two circle/bubble centers and radii.
//             SKPoint center1 = new SKPoint(x1, 50);
//             SKPoint center2 = new SKPoint(x2, 60);
//             float radius1 = 65;
//             float radius2 = 45;

//             // Create paths for circles
//             var circlePath1 = new SKPath();
//             circlePath1.AddCircle(center1.X, center1.Y, radius1);

//             var circlePath2 = new SKPath();
//             circlePath2.AddCircle(center2.X, center2.Y, radius2);

//             // Option 1: Use path operations to merge the shapes
//             // Note: You may need to craft the connecting "bridge" more deliberately for a liquid effect.
//             var combinedPath = new SKPath();
//             combinedPath.AddPath(circlePath1);
//             combinedPath.AddPath(circlePath2);

//             // Option 2: Compute a "metaball" connection manually:
//             // You can compute control points based on the positions and radii,
//             // then create a path that smoothly connects the two circles.
//             SKPath metaballPath = CreateMetaballPath(center1, radius1, center2, radius2);

//             // Use a blur filter for soft edges (adjust blurRadius as needed)
//             float blurRadius = 5.0f;
//             paint.ImageFilter = SKImageFilter.CreateBlur(blurRadius, blurRadius);

//             // Draw the metaball shape with the blurred paint for smoothness
//             canvas.DrawPath(metaballPath, paint);

//             // If you want a colored version:
//             // Step 1: Render the shape as a mask into a separate surface.
//             using (var maskSurface = SKSurface.Create(info))
//             {
//                 SKCanvas maskCanvas = maskSurface.Canvas;
//                 maskCanvas.Clear(SKColors.Transparent);
//                 // Render the shape onto the mask surface (as a white shape on transparency)
//                 var maskPaint = new SKPaint
//                 {
//                     IsAntialias = true,
//                     Style = SKPaintStyle.Fill,
//                     Color = SKColors.White
//                 };
//                 maskCanvas.DrawPath(metaballPath, maskPaint);
//                 // Retrieve the mask image
//                 using (var maskImage = maskSurface.Snapshot())
//                 {
//                     // Now composite the color over your desired background using the mask
//                     // For example, draw a colored rectangle and then apply the mask
//                     var colorPaint = new SKPaint
//                     {
//                         IsAntialias = true,
//                         Color = SKColors.Red  // Your chosen color here
//                     };
//                     // Draw your colored background (or shape)
//                     canvas.DrawRect(new SKRect(0, 0, info.Width, info.Height), colorPaint);

//                     // Now use the mask image as a shader or clipping mask
//                     // One approach is to use the mask image with a blend mode
//                     var maskFilterPaint = new SKPaint
//                     {
//                         BlendMode = SKBlendMode.DstIn,
//                         IsAntialias = true
//                     };
//                     canvas.DrawImage(maskImage, 0, 0, maskFilterPaint);
//                 }
//             }

//             canvas.RestoreToCount(layer);
//         }


//         SKPath CreateMetaballPath(SKPoint center1, float radius1, SKPoint center2, float radius2)
//         {
//             var path = new SKPath();

//             // Calculate distance between centers
//             float dx = center2.X - center1.X;
//             float dy = center2.Y - center1.Y;
//             float d = (float)Math.Sqrt(dx * dx + dy * dy);

//             // Determine threshold, handle when circles are far apart (or overlapping)
//             // The following values and math are examples; you'll need to adjust based on the desired "liquid" appearance.
//             float handleLength = Math.Min(radius1, radius2) * 0.6f;

//             // Compute the angle between circles.
//             float angle = (float)Math.Atan2(dy, dx);

//             // Compute connection points on the perimeter of each circle.
//             SKPoint p1 = new SKPoint(center1.X + radius1 * (float)Math.Cos(angle),
//                                      center1.Y + radius1 * (float)Math.Sin(angle));
//             SKPoint p2 = new SKPoint(center2.X - radius2 * (float)Math.Cos(angle),
//                                      center2.Y - radius2 * (float)Math.Sin(angle));

//             // Control points to create the smooth bridge:
//             SKPoint cp1 = new SKPoint(p1.X + handleLength * (float)Math.Cos(angle),
//                                       p1.Y + handleLength * (float)Math.Sin(angle));
//             SKPoint cp2 = new SKPoint(p2.X - handleLength * (float)Math.Cos(angle),
//                                       p2.Y - handleLength * (float)Math.Sin(angle));

//             // Build the metaball path (this is a basic example for one side)
//             path.MoveTo(p1);
//             path.CubicTo(cp1, cp2, p2);

//             // Mirror for the other side:
//             // Calculate the opposite angles for the return path
//             float angleOpp = angle + (float)Math.PI;
//             SKPoint p3 = new SKPoint(center2.X - radius2 * (float)Math.Cos(angleOpp),
//                                      center2.Y - radius2 * (float)Math.Sin(angleOpp));
//             SKPoint p4 = new SKPoint(center1.X + radius1 * (float)Math.Cos(angleOpp),
//                                      center1.Y + radius1 * (float)Math.Sin(angleOpp));
//             SKPoint cp3 = new SKPoint(p3.X + handleLength * (float)Math.Cos(angleOpp),
//                                       p3.Y + handleLength * (float)Math.Sin(angleOpp));
//             SKPoint cp4 = new SKPoint(p4.X - handleLength * (float)Math.Cos(angleOpp),
//                                       p4.Y - handleLength * (float)Math.Sin(angleOpp));

//             path.LineTo(p3);
//             path.CubicTo(cp3, cp4, p4);
//             path.Close();

//             return path;
//         }
//     }
// }