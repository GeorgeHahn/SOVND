using System;
using System.Threading;
using Anotar.NLog;
using NAudio.Wave;
using Newtonsoft.Json;
using SOVND.Client.Audio;
using SOVND.Client.Util;
using SOVND.Lib.Models;
using SpotifyClient;
using System.Threading.Tasks;
using SOVND.Lib.Utils;

namespace SOVND.Client.Modules
{
    public class NowPlayingHandler : SOVNDModule
    {
        private Track playingTrack;
        private CancellationTokenSource songToken;
        private SpotifyTrackDataPipe streamingaudio;
        private readonly string _channel;

        private bool ASongHasPlayed;
        private long? ServerLag;

        public Action<string, bool> PlayingSongChanged_Toast { get; set; }
        public Action<string, bool> ScrobbleSong { get; set; }

        private readonly DateTime UnixTimeBase = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        const int PrefetchTime = 1000;

        public NowPlayingHandler(AuthPair auth, string channelName) : base(auth)
        {
            _channel = channelName;
            
            On["/" + channelName + "/nowplaying"] = _ =>
            {
                if (string.IsNullOrEmpty(_.Message) || _.Message == "remove")
                {
                    if(_waveOut != null)
                        _waveOut.Stop();
                    return;
                }

                Task.Run(() =>
                {
                    NowPlaying song = JsonConvert.DeserializeObject<NowPlaying>(_.Message);

                    if (!ASongHasPlayed)
                    {
                        // This is the first song to play in this channel (probably a partial)
                        ASongHasPlayed = true;
                    }
                    else if (ServerLag == null)
                    {
                        // This is the second song - capture the lag offset
                        ServerLag = Time.Timestamp() - song.votetime;
                        LogTo.Error("Server lag set to {0}ms", ServerLag.Value);
                    }
                    else
                    {
                        // This is the third song or later, adjust the time offset
                        song.votetime += ServerLag.Value;
                    }
                    
                    song.votetime += PrefetchTime;

                    if (string.IsNullOrWhiteSpace(song.songID))
                    {
                        playingTrack = null;
                        LogTo.Warn("Server asked to play empty song on channel {0}", _channel);
                        return;
                    }

                    PlaySong(song.songID, UnixTimeBase.AddMilliseconds(song.votetime));
                });
            };

            Run();
        }

        static WaveOut _waveOut;
        private static BufferedWaveProvider _thisBuffer;

        private void PlaySong(string songID)
        {
            PlaySong(songID, DateTime.MinValue);
        }

        private void PlaySong(string songID, DateTime startTime)
        {
            LogTo.Debug("Playing: {0}", songID);
            Logging.Event("Played song");

            streamingaudio = new SpotifyTrackDataPipe();
            playingTrack = new Track(songID, true);

            streamingaudio.StartStreaming(startTime, playingTrack.TrackPtr,
                () =>
                {
                    LogTo.Trace("NPH: _waveOut.Play()");
                    if (PlayingSongChanged_Toast != null)
                        PlayingSongChanged_Toast(songID, true);
                    if (ScrobbleSong != null)
                        ScrobbleSong(songID, true);
                    if (_waveOut != null)
                        _waveOut.Play();
                },
                buffer =>
                {
                    LogTo.Trace("NPH: Initialize buffer");
                    if (buffer == _thisBuffer)
                    {
                        LogTo.Trace("NPH: Initialize buffer: already initialized");
                        buffer.ClearBuffer();
                        return;
                    }

                    LogTo.Trace("NPH: Initialize buffer: initializing");
                    _waveOut = new WaveOut(App.WindowHandle);
                    _waveOut.Init(buffer);

                    _thisBuffer = buffer;
                },
                () =>
                {
                    LogTo.Trace("NPH: _waveOut.Pause()");
                    if (PlayingSongChanged_Toast != null)
                        PlayingSongChanged_Toast(songID, false);
                    if (ScrobbleSong != null)
                        ScrobbleSong(songID, false);
                    if (_waveOut != null)
                        _waveOut.Stop();
                });
        }

        public void StopStreaming()
        {
            if(streamingaudio != null)
                streamingaudio.Dispose();
        }

        protected override void OnStop()
        {
            StopStreaming();
        }
    }
}