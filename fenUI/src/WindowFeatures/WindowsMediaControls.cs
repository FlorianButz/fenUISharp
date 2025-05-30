using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using FenUISharp;
using SkiaSharp;
using Windows.Foundation;
using Windows.Media;
using Windows.Media.Control;
using Windows.Storage.Streams;

namespace FenUISharp.WinFeatures
{
    public struct PlaybackInfo
    {
        public bool isActiveSession;

        public bool? isShuffling;
        public GlobalSystemMediaTransportControlsSessionPlaybackStatus? playbackState;
        public MediaPlaybackAutoRepeatMode? repeatMode;
        public MediaPlaybackType? mediaType;

        public string title;
        public string album;
        public string artist;
        public string albumArtist;

        public string? sourceAppModelId;

        public double duration;
        public double position;

        public SKImage? thumbnail;

        public PlaybackInfo() : this(false) { }

        public PlaybackInfo(bool init)
        {
            isActiveSession = false;
            isShuffling = false;
            playbackState = GlobalSystemMediaTransportControlsSessionPlaybackStatus.Stopped;
            repeatMode = MediaPlaybackAutoRepeatMode.None;
            mediaType = MediaPlaybackType.Unknown;
            title = "";
            album = "";
            artist = "";
            albumArtist = "";
            sourceAppModelId = null;
            duration = 0;
            position = 0;
            thumbnail = null;
        }
    }

    public class WindowsMediaControls
    {
        static GlobalSystemMediaTransportControlsSessionManager? globSessionManager;
        static GlobalSystemMediaTransportControlsSession? currentSession;

        static PlaybackInfo cachedInfo = new PlaybackInfo();
        public static PlaybackInfo CachedInfo => cachedInfo;

        public static Action? onMediaUpdated { get; set; }
        public static Action? onThumbnailUpdated { get; set; }
        public static Action? onTimelineUpdated { get; set; }

        static bool continousPolling = true;
        public static bool ContinousPolling
        {
            get => continousPolling;
            set
            {
                if (globSessionManager != null && continousPolling != value)
                {
                    if (value)
                        globSessionManager.SessionsChanged -= SessionChangedHandler;
                    else
                        globSessionManager.SessionsChanged += SessionChangedHandler;
                }
                continousPolling = value;
            }
        }

        public WindowsMediaControls()
        {
            InitWindowsMediaControls();
            PollMediaInfoThread();
        }

        static void PollMediaInfoThread()
        {
            var thread = new Thread(() =>
            {
                while (true)
                {
                    if (ContinousPolling)
                    {
                        currentSession = globSessionManager?.GetCurrentSession();
                        if (currentSession != null)
                        {
                            try
                            {
                                UpdateInfo().Wait();
                            } catch(Exception e) { continue; }
                        }
                        Thread.Sleep(500);
                    }
                    else
                    {
                        Thread.Sleep(1000);
                    }
                }
            });
            thread.IsBackground = true;
            thread.Start();
        }

        private static async void InitWindowsMediaControls()
        {
            globSessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();

            if (!ContinousPolling)
            {
                globSessionManager.SessionsChanged += SessionChangedHandler;
            }

            TrySubscribeToCurrentSession();
            await UpdateInfo();
        }

        private static void SessionChangedHandler(GlobalSystemMediaTransportControlsSessionManager sender, SessionsChangedEventArgs args)
        {
            TrySubscribeToCurrentSession();
        }

        public static async Task UpdateInfo()
        {
            currentSession ??= globSessionManager?.GetCurrentSession();

            if (currentSession != null)
            {
                var info = await currentSession.TryGetMediaPropertiesAsync();
                var playbackInfo = currentSession.GetPlaybackInfo();
                var timelineProperties = currentSession.GetTimelineProperties();

                bool infoChanged = info.Title != cachedInfo.title || info.Artist != cachedInfo.artist;

                cachedInfo.title = info.Title;
                cachedInfo.artist = info.Artist;
                cachedInfo.album = info.AlbumTitle;
                cachedInfo.albumArtist = info.AlbumArtist;
                cachedInfo.sourceAppModelId = currentSession.SourceAppUserModelId;

                if (playbackInfo != null)
                {
                    cachedInfo.isShuffling = playbackInfo.IsShuffleActive;
                    cachedInfo.repeatMode = playbackInfo.AutoRepeatMode;
                    cachedInfo.playbackState = playbackInfo.PlaybackStatus;
                    cachedInfo.mediaType = playbackInfo.PlaybackType;
                }

                if (timelineProperties != null)
                {
                    TimeSpan duration = timelineProperties.EndTime - timelineProperties.StartTime;
                    bool isUpdatedTimeline = cachedInfo.duration != duration.TotalSeconds ||
                                             cachedInfo.position != timelineProperties.Position.TotalSeconds;
                    cachedInfo.duration = duration.TotalSeconds;
                    cachedInfo.position = timelineProperties.Position.TotalSeconds;
                    cachedInfo.isActiveSession = true;

                    if (isUpdatedTimeline && ContinousPolling)
                        onTimelineUpdated?.Invoke();
                }

                if (infoChanged && ContinousPolling)
                {
                    var thumbnail = info.Thumbnail;
                    if (thumbnail != null)
                    {
                        using var stream = await thumbnail.OpenReadAsync();
                        cachedInfo.thumbnail = ConvertThumbnailToSkImage(stream);
                        onThumbnailUpdated?.Invoke();
                    }

                    onMediaUpdated?.Invoke();
                }
            }
            else
            {
                cachedInfo = new PlaybackInfo();
            }
        }

        private const int THUMBNAIL_SIZE = 512;

        private static SKImage? ConvertThumbnailToSkImage(IRandomAccessStreamWithContentType thumbnailStream)
        {
            using var skStream = new SKManagedStream(thumbnailStream.AsStream());
            var originalBitmap = SKBitmap.Decode(skStream);
            if (originalBitmap == null) return null;

            var resizedBitmap = originalBitmap.Resize(new SKImageInfo(THUMBNAIL_SIZE, THUMBNAIL_SIZE), SKFilterQuality.High);
            originalBitmap.Dispose();

            return resizedBitmap != null ? SKImage.FromBitmap(resizedBitmap) : null;
        }

        static void TrySubscribeToCurrentSession()
        {
            var newSession = globSessionManager?.GetCurrentSession();

            if (newSession == currentSession) return;

            if (currentSession != null)
            {
                currentSession.MediaPropertiesChanged -= MediaPropertiesChangedHandler;
                currentSession.PlaybackInfoChanged -= PlaybackInfoChangedHandler;
            }

            currentSession = newSession;

            if (currentSession != null)
            {
                currentSession.MediaPropertiesChanged += MediaPropertiesChangedHandler;
                currentSession.PlaybackInfoChanged += PlaybackInfoChangedHandler;
            }
        }

        private static async void MediaPropertiesChangedHandler(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
        {
            await UpdateInfo();
            onMediaUpdated?.Invoke();
        }

        private static async void PlaybackInfoChangedHandler(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
        {
            await UpdateInfo();
            onTimelineUpdated?.Invoke();
        }

        public static void TriggerMediaControl(MediaControlTrigger trigger)
        {
            Task.Run(async () =>
            {
                if (currentSession == null) return;

                var playbackInfo = currentSession.GetPlaybackInfo();
                switch (trigger)
                {
                    case MediaControlTrigger.Play:
                        await currentSession.TryPlayAsync();
                        break;
                    case MediaControlTrigger.Stop:
                        await currentSession.TryStopAsync();
                        break;
                    case MediaControlTrigger.PlayPauseToggle:
                        await currentSession.TryTogglePlayPauseAsync();
                        break;
                    case MediaControlTrigger.SkipNext:
                        await currentSession.TrySkipNextAsync();
                        break;
                    case MediaControlTrigger.SkipPrevious:
                        await currentSession.TrySkipPreviousAsync();
                        break;
                    case MediaControlTrigger.ToggleShuffle:
                        if (playbackInfo != null)
                            await currentSession.TryChangeShuffleActiveAsync(!playbackInfo.IsShuffleActive.GetValueOrDefault());
                        break;
                    case MediaControlTrigger.SwapLoopMode:
                        if (playbackInfo != null)
                        {
                            MediaPlaybackAutoRepeatMode nextMode = playbackInfo.AutoRepeatMode switch
                            {
                                MediaPlaybackAutoRepeatMode.None => MediaPlaybackAutoRepeatMode.Track,
                                MediaPlaybackAutoRepeatMode.Track => MediaPlaybackAutoRepeatMode.List,
                                _ => MediaPlaybackAutoRepeatMode.None
                            };
                            await currentSession.TryChangeAutoRepeatModeAsync(nextMode);
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
