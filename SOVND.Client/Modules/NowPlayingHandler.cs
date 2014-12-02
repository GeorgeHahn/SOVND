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
        private static object soundlock = new object();

        private readonly string _channel;

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

                PlaySong(song);
            };

            Run();
        }

        private void PlaySong(string songID)
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

            Task.Factory.StartNew(() =>
            {
                lock (soundlock)
                {
                    using (var player = new WaveOut(App.WindowHandle))
                    {
                        playingTrack = new Track(songID);

                        wave = new BufferedWaveProvider(WaveFormat);
                        wave.BufferDuration = TimeSpan.FromSeconds(15);

                        player.Init(wave);
                        streamingaudio = new SpotifyTrackDataPipe(playingTrack.TrackPtr, wave);
                        player.Play();

                        while (!streamingaudio.Complete && !token.IsCancellationRequested)
                        {
                            Thread.Sleep(15);
                        }

                        streamingaudio.StopStreaming();
                        player.Pause();
                    }
                }
            }, token);
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