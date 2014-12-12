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

        static WaveOut _waveOut;
        private bool _initialized;
        private BufferedWaveProvider _thisBuffer;

        private void PlaySong(string songID)
        {
            PlaySong(songID, DateTime.MinValue);
        }

        private void PlaySong(string songID, DateTime startTime)
        {
            LogTo.Debug("Playing: {0}", songID);
            if (playingTrack?.SongID == songID)
                return;

            streamingaudio = new SpotifyTrackDataPipe();

            playingTrack = new Track(songID);

            streamingaudio.StartStreaming(startTime, playingTrack.TrackPtr,
                () =>
                {
                    LogTo.Trace("NPH: _waveOut.Play()");
                    _waveOut.Play();
                },
                buffer =>
                {
                    LogTo.Trace("NPH: Initialize buffer");
                    if (buffer != _thisBuffer)
                        _initialized = false;

                    if (_initialized)
                    {
                        LogTo.Trace("NPH: Initialize buffer: already initialized");
                        return;
                    }

                    LogTo.Trace("NPH: Initialize buffer: initializing");
                    _waveOut = new WaveOut(App.WindowHandle);
                    _waveOut.Init(buffer);
                    _thisBuffer = buffer;
                    _initialized = true;
                },
                () =>
                {
                    LogTo.Trace("NPH: _waveOut.Pause()");
                    _waveOut.Pause();
                });
        }

        public void StopStreaming()
        {
            streamingaudio?.Dispose();
        }

        protected override void OnStop()
        {
            StopStreaming();
        }
    }
}