using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using FenUISharp;
using SkiaSharp;
using Windows.Foundation;
using Windows.Media;
using Windows.Media.Control;
using Windows.Storage.Streams;

namespace FenUISharp
{
    public struct PlaybackInfo
    {
        public bool isActiveSession = false;

        public bool? isShuffling = false;
        public GlobalSystemMediaTransportControlsSessionPlaybackStatus? playbackState = GlobalSystemMediaTransportControlsSessionPlaybackStatus.Stopped;
        public MediaPlaybackAutoRepeatMode? repeatMode = MediaPlaybackAutoRepeatMode.None;
        public MediaPlaybackType? mediaType = MediaPlaybackType.Unknown;

        public string title = "";
        public string album = "";
        public string artist = "";
        public string albumArtist = "";

        public string? sourceAppModelId;

        public double duration = 0;
        public double position = 0;

        public SKImage? thumbnail;

        public PlaybackInfo() { }
    }

    public class WindowsMediaControls
    {
        static GlobalSystemMediaTransportControlsSessionManager? globSessionManager;
        static GlobalSystemMediaTransportControlsSession? currentSession;

        static PlaybackInfo cachedInfo;
        public static PlaybackInfo CachedInfo { get => cachedInfo; }

        public static Action? onMediaUpdated { get; set; }
        public static Action? onThumbnailUpdated { get; set; }
        public static Action? onTimelineUpdated { get; set; }

        static bool continousPolling = true;
        public static bool ContinousPolling
        {
            get { return continousPolling; }
            set
            {
                continousPolling = value;
                if (continousPolling)
                    globSessionManager.SessionsChanged -= (s, e) => TrySubscribeToCurrentSession();
                else
                    globSessionManager.SessionsChanged += (s, e) => TrySubscribeToCurrentSession();
            }
        }

        public WindowsMediaControls()
        {
            InitWindowsMediaControls();
            Task.Run(() => PollMediaInfoAsync());
        }

        static async Task PollMediaInfoAsync()
        {
            while (true)
            {
                currentSession = globSessionManager?.GetCurrentSession();
                if (currentSession != null && ContinousPolling)
                {
                    await UpdateInfo();
                }
                await Task.Delay(2000);
            }
        }

        private static async void InitWindowsMediaControls()
        {
            globSessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();

            if (!ContinousPolling)
            {
                globSessionManager.SessionsChanged += (s, e) =>
                {
                    TrySubscribeToCurrentSession();
                };

                TrySubscribeToCurrentSession();
            }

            await UpdateInfo();
        }

        public static async Task UpdateInfo()
        {
            if (currentSession != null)
            {
                var info = await currentSession?.TryGetMediaPropertiesAsync();
                var playbackInfo = currentSession?.GetPlaybackInfo();

                if (info.Title != cachedInfo.title || info.Artist != cachedInfo.artist)
                {
                    cachedInfo.title = info.Title;
                    cachedInfo.artist = info.Artist;
                    cachedInfo.album = info.AlbumTitle;
                    cachedInfo.albumArtist = info.AlbumArtist;

                    cachedInfo.sourceAppModelId = currentSession?.SourceAppUserModelId;

                    if (playbackInfo != null)
                    {
                        cachedInfo.isShuffling = playbackInfo.IsShuffleActive;
                        cachedInfo.repeatMode = playbackInfo.AutoRepeatMode;
                    }

                    var thumbnail = info.Thumbnail;
                    if (thumbnail != null)
                    {
                        using (IRandomAccessStreamWithContentType stream = await thumbnail.OpenReadAsync())
                        {
                            cachedInfo.thumbnail = await ConvertThumbnailToSkImage(stream);
                            onThumbnailUpdated?.Invoke();
                        }
                    }

                    if (ContinousPolling) onMediaUpdated?.Invoke();
                }

                if (playbackInfo != null)
                {
                    cachedInfo.mediaType = playbackInfo.PlaybackType;
                    cachedInfo.playbackState = playbackInfo.PlaybackStatus;
                }

                var timelineProperties = currentSession?.GetTimelineProperties();
                TimeSpan duration = timelineProperties.EndTime - timelineProperties.StartTime;

                bool isUpdatedTimeline = false;
                if (cachedInfo.duration != duration.TotalSeconds || cachedInfo.position != timelineProperties.Position.TotalSeconds) isUpdatedTimeline = true;
                cachedInfo.duration = duration.TotalSeconds;
                cachedInfo.position = timelineProperties.Position.TotalSeconds;

                if (isUpdatedTimeline && ContinousPolling)
                    onTimelineUpdated?.Invoke();

                cachedInfo.isActiveSession = true;
            }
            else
            {
                cachedInfo = new PlaybackInfo();
            }
        }

        // Returns thumbnail as 128x128 SKImage
        private static async Task<SKImage?> ConvertThumbnailToSkImage(IRandomAccessStreamWithContentType thumbnailStream)
        {
            using (var skStream = new SKManagedStream(thumbnailStream.AsStream()))
            {
                var originalBitmap = SKBitmap.Decode(skStream);
                if (originalBitmap == null) return null;

                var resizedBitmap = originalBitmap.Resize(new SKImageInfo(128, 128), Window.samplingOptions);
                originalBitmap.Dispose();

                using (resizedBitmap) { return SKImage.FromBitmap(resizedBitmap); }
                ;
            }
        }

        static void TrySubscribeToCurrentSession()
        {
            if (currentSession != null && globSessionManager?.GetCurrentSession() != null)
            {
                currentSession.MediaPropertiesChanged -= MediaPropertiesChangedHandler;
                currentSession.PlaybackInfoChanged -= PlaybackInfoChangedHandler;
            }

            currentSession = globSessionManager?.GetCurrentSession();

            if (currentSession != null)
            {
                currentSession.MediaPropertiesChanged += MediaPropertiesChangedHandler;
                currentSession.PlaybackInfoChanged += PlaybackInfoChangedHandler;
            }
        }

        private static async void MediaPropertiesChangedHandler(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
        {
            var mediaProps = await sender.TryGetMediaPropertiesAsync();
            await UpdateInfo(); onMediaUpdated?.Invoke();
        }

        private static void PlaybackInfoChangedHandler(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
        {
            var playbackInfo = sender.GetPlaybackInfo();
            Task.Run(async () => { await UpdateInfo(); onTimelineUpdated?.Invoke(); });
        }



        public static void TriggerMediaControl(MediaControlTrigger trigger)
        {
            Task.Run(async () =>
            {
                switch (trigger)
                {
                    case MediaControlTrigger.Play:
                        await currentSession?.TryPlayAsync();
                        break;
                    case MediaControlTrigger.Stop:
                        await currentSession?.TryStopAsync();
                        break;
                    case MediaControlTrigger.PlayPauseToggle:
                        await currentSession?.TryTogglePlayPauseAsync();
                        break;
                    case MediaControlTrigger.SkipNext:
                        await currentSession?.TrySkipNextAsync();
                        break;
                    case MediaControlTrigger.SkipPrevious:
                        await currentSession?.TrySkipPreviousAsync();
                        break;
                    case MediaControlTrigger.ToggleShuffle:
                        var pIs = currentSession?.GetPlaybackInfo();
                        if (pIs != null)
                            await currentSession?.TryChangeShuffleActiveAsync(!pIs.IsShuffleActive.GetValueOrDefault());
                        break;
                    case MediaControlTrigger.SwapLoopMode:
                        var pIl = currentSession?.GetPlaybackInfo();
                        if (pIl != null)
                            switch (cachedInfo.repeatMode)
                            {
                                case MediaPlaybackAutoRepeatMode.None:
                                    currentSession?.TryChangeAutoRepeatModeAsync(MediaPlaybackAutoRepeatMode.Track);
                                    cachedInfo.repeatMode = MediaPlaybackAutoRepeatMode.Track;
                                    break;
                                case MediaPlaybackAutoRepeatMode.Track:
                                    currentSession?.TryChangeAutoRepeatModeAsync(MediaPlaybackAutoRepeatMode.List);
                                    cachedInfo.repeatMode = MediaPlaybackAutoRepeatMode.List;
                                    break;
                                case MediaPlaybackAutoRepeatMode.List:
                                    currentSession?.TryChangeAutoRepeatModeAsync(MediaPlaybackAutoRepeatMode.None);
                                    cachedInfo.repeatMode = MediaPlaybackAutoRepeatMode.None;
                                    break;
                            }
                        break;
                }

                await UpdateInfo();
            });
        }
    }

    public enum MediaControlTrigger
    {
        Play, PlayPauseToggle, Stop,

        SkipNext, SkipPrevious,

        ToggleShuffle, SwapLoopMode
    }
}