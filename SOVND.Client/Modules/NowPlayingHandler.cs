using System;
using System.Threading;
using System.Threading.Tasks;
using Anotar.NLog;
using NAudio.Wave;
using SOVND.Client.Audio;
using SOVND.Client.Util;
using SpotifyClient;

namespace SOVND.Client.Modules
{
    public class NowPlayingHandler : SOVNDModule
    {
        private Track playingTrack;
        private CancellationTokenSource songToken;
        private SpotifyTrackDataPipe streamingaudio;
        private readonly string _channel;

        private readonly DateTime UnixTimeBase = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        public NowPlayingHandler(AuthPair auth, string channelName) : base(auth)
        {
            // TODO Use channel/nowplaying/starttime to seek to correct position
            // TODO Convert nowplaying to a JSON object so songid and playtime come in at the same time?

            _channel = channelName;
            streamingaudio = new SpotifyTrackDataPipe();

            On["/" + channelName + "/nowplaying/songid"] = _ =>
            {
                string song = _.Message;
                if (string.IsNullOrWhiteSpace(song))
                {
                    playingTrack = null;
                    LogTo.Warn("Server asked to play empty song on channel {0}", _channel);
                    OnStop(); // TODO Danger danger
                    return;
                }
                int time = 0;

                //PlaySong(song, UnixTimeBase.AddSeconds(time).ToLocalTime());
                Task.Run(() => PlaySong(song));
            };

            Run();
        }

        private async Task PlaySong(string songID)
        {
            await PlaySong(songID, DateTime.MinValue);
        }

        private async Task PlaySong(string songID, DateTime startTime)
        {
            LogTo.Debug("Playing: {0}", songID);

            if (playingTrack?.SongID == songID)
                return;

            if (streamingaudio != null)
            {
                streamingaudio.DoComplete();
                streamingaudio = new SpotifyTrackDataPipe();
            }

            playingTrack = new Track(songID);

            WaveOut player = new WaveOut(App.WindowHandle);

            LogTo.Trace("Streaming");

            streamingaudio.StartStreaming(startTime, playingTrack.TrackPtr, provider =>
            {
                player.Init(provider);
                player.Play();
            });

            streamingaudio.OnComplete = () =>
            {
                LogTo.Trace("Done streaming");

                streamingaudio.StopStreaming();
                player.Pause();
                player.Dispose();
            };
        }

        protected override void OnStop()
        {
            if (streamingaudio != null)
                streamingaudio.DoComplete();
            streamingaudio = null;
        }
    }
}