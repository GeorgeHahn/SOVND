using System;
using System.Threading;
using System.Threading.Tasks;
using Anotar.NLog;
using NAudio.Wave;
using Newtonsoft.Json;
using SOVND.Client.Audio;
using SOVND.Client.Util;
using SOVND.Lib.Models;
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
            _channel = channelName;
            
            On["/" + channelName + "/nowplaying"] = _ =>
            {
                NowPlaying song = JsonConvert.DeserializeObject<NowPlaying>(_.Message);

                StopStreaming();

                if (string.IsNullOrWhiteSpace(song.songID))
                {
                    playingTrack = null;
                    LogTo.Warn("Server asked to play empty song on channel {0}", _channel);
                    return;
                }

                Task.Run(() => PlaySong(song.songID, UnixTimeBase.AddMilliseconds(song.votetime)));
            };

            Run();
        }

        private void PlaySong(string songID)
        {
            PlaySong(songID, DateTime.MinValue);
        }

        private void PlaySong(string songID, DateTime startTime)
        {
            LogTo.Debug("Playing: {0}", songID);
            if (playingTrack?.SongID == songID)
                return;

            if (streamingaudio != null)
                streamingaudio.DoComplete();

            streamingaudio = new SpotifyTrackDataPipe();
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

        public void StopStreaming()
        {
            if (streamingaudio != null)
                streamingaudio.DoComplete();
            streamingaudio = null;
        }

        protected override void OnStop()
        {
            StopStreaming();
        }
    }
}