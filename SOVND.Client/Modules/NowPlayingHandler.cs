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
        private SpotifyTrackDataPipe streamingaudio;
        private BufferedWaveProvider wave;
        private WaveFormat WaveFormat;
        private CancellationTokenSource songToken;

        private readonly string _channel;

        private readonly DateTime UnixTimeBase = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        public NowPlayingHandler(AuthPair auth, string channelName) : base(auth)
        {
            // TODO Use channel/nowplaying/starttime to seek to correct position
            // TODO Convert nowplaying to a JSON object so songid and playtime come in at the same time?

            _channel = channelName;

            On["/" + channelName + "/nowplaying/songid"] = _ =>
            {
                string song = _.Message;
                if (string.IsNullOrWhiteSpace(song))
                {
                    playingTrack = null;
                    LogTo.Warn("Server asked to play empty song on channel {0}", _channel);
                    return;
                }
                int time = 0;

                //PlaySong(song, UnixTimeBase.AddSeconds(time).ToLocalTime());
                PlaySong(song);
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

            if (WaveFormat == null) // TODO Get song format from Spotify
                WaveFormat = new WaveFormat(44100, 16, 2);

            if (songToken != null)
                songToken.Cancel();

            songToken = new CancellationTokenSource();
            var token = songToken.Token;

            using (var player = new WaveOut(App.WindowHandle))
            {
                playingTrack = new Track(songID);

                wave = new BufferedWaveProvider(WaveFormat);
                wave.BufferDuration = TimeSpan.FromSeconds(15);

                player.Init(wave);
                streamingaudio = new SpotifyTrackDataPipe(playingTrack.TrackPtr, wave);
                await streamingaudio.StartStreaming(startTime);
                player.Play();

                while (!streamingaudio.Complete && !token.IsCancellationRequested)
                {
                    await Task.Delay(50, token);
                }

                streamingaudio.StopStreaming();
                player.Pause();
            }
        }

        protected override void OnStop()
        {
            if (songToken != null)
                songToken.Cancel();

            if (streamingaudio != null)
                streamingaudio.StopStreaming();
        }
    }
}