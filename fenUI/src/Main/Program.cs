using System.Reflection;
using FenUISharp;
using SkiaSharp;

using Windows.Media.Control;
using Windows.Storage.Streams;

namespace FenUISharpTest1
{
    class Program
    {
        [STAThread]
        static void Main(){
            FWindow window = new FWindow("fenUISharp Test", "fenUISharpTest");
            window.Show();

            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icons", "TrayIcon.ico");
            window.SetWindowIcon(iconPath);
            window.CreateTray(iconPath, "Test tray icon!");
            
            FWindow.onTrayIconRightClicked += () => {
                Console.WriteLine("Tray clicked!");
            };

            var t = new TestComponent(0, 0, 500, 350);
            // t.skPaint.ImageFilter = SKImageFilter.CreateDropShadow(0, 0, 25, 25, SKColors.Black);

            // var t2 = new TestComponent(0, 0, 350, 50);
            // t2.transform.scale = new Vector2(0.25f, 0.25f);
            // t2.skPaint.Color = SKColors.Red;
            // //t2.transform.alignment = new Vector2(0.5f, 0);
            // t2.transform.parent = t.transform;

            // var t3 = new TestComponent(0, 0, 150, 15);
            // t3.transform.scale = new Vector2(0.35f, 0.15f);
            // t3.skPaint.Color = SKColors.Yellow;
            // //t2.transform.alignment = new Vector2(0.5f, 0);
            // t3.transform.parent = t2.transform;

            FWindow.uiComponents.Add(t);
            // FWindow.uiComponents.Add(t2);
            // FWindow.uiComponents.Add(t3);

            // for (int i = 0; i < 1500; i++) {
            //     FWindow.uiComponents.Add(new TestComponent());
            // }

            window.CreateSurface();
            window.Begin();
        }


        // MEDIA TRANSPORT CONTROLS FINALLY WORK - YAYY YES I AM SO HAPPY RIGHT NOW

// static async Task Main(string[] args)
//         {
//             // Request the session manager (requires Windows 10 1803 or later)
//             var sessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
//             var currentSession = sessionManager.GetCurrentSession();

//             if (currentSession == null)
//             {
//                 Console.WriteLine("No active media session found.");
//                 return;
//             }

//             // Get media properties such as Title, Album, Artist
//             var mediaProperties = await currentSession.TryGetMediaPropertiesAsync();
//             Console.WriteLine("Title: " + mediaProperties.Title);
//             Console.WriteLine("Album: " + mediaProperties.AlbumTitle);
//             Console.WriteLine("Artist: " + mediaProperties.Artist);

//             // Get timeline properties for duration and current position
//             var timelineProperties = currentSession.GetTimelineProperties();
//             TimeSpan duration = timelineProperties.EndTime - timelineProperties.StartTime;
//             Console.WriteLine("Duration: " + duration);
//             Console.WriteLine("Current Position: " + timelineProperties.Position);

//             // Retrieve the album art (icon)
//             var thumbnail = mediaProperties.Thumbnail;
//             if (thumbnail != null)
//             {
//                 IRandomAccessStreamWithContentType stream = await thumbnail.OpenReadAsync();

//                 // For example, write the thumbnail to a file (requires System.IO)
//                 using (var fileStream = new FileStream("thumbnail.jpg", FileMode.Create, FileAccess.Write))
//                 {
//                     await stream.AsStreamForRead().CopyToAsync(fileStream);
//                 }
//                 Console.WriteLine("Thumbnail saved as thumbnail.jpg");
//             }
//             else
//             {
//                 Console.WriteLine("No thumbnail available.");
//             }
//         }
    }
}