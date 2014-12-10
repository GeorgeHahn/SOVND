/*-
 * Copyright (c) 2014 Software Development Solutions, Inc.
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 * 1. Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in the
 *    documentation and/or other materials provided with the distribution.
 *
 * THIS SOFTWARE IS PROVIDED BY THE AUTHOR AND CONTRIBUTORS ``AS IS'' AND
 * ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED.  IN NO EVENT SHALL THE AUTHOR OR CONTRIBUTORS BE LIABLE
 * FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
 * DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS
 * OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
 * HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
 * LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY
 * OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF
 * SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Anotar.NLog;
using libspotifydotnet.libspotify;
using NAudio.Wave;
using SpotifyClient;

namespace SOVND.Client.Audio
{
    public class SpotifyTrackDataPipe
    {
        private bool _complete;
        private static bool _loaded;
        private BufferedWaveProvider _wave;
        private bool logonce;
        private WaveFormat waveFormat;
        private Action<BufferedWaveProvider> _init;

        public bool Complete { get { return _complete; } }

        public async Task StartStreaming(IntPtr _trackPtr, Action<BufferedWaveProvider> init)
        {
            await StartStreaming(DateTime.MinValue, _trackPtr, init);
        }

        public async Task StartStreaming(DateTime startTime, IntPtr _trackPtr, Action<BufferedWaveProvider> init)
        {
            if (_loaded)
            {
                StopStreaming();
                _loaded = false;
            }
            logonce = false;

            var error = Session.LoadPlayer(_trackPtr);

            while (error == sp_error.IS_LOADING)
            {
                await Task.Delay(50);
                error = Session.LoadPlayer(_trackPtr);
            }

            if (error != sp_error.OK)
            {
                throw new Exception("[Spotify] Streaming error: \{sp_error_message(error)}");
            }

            _loaded = true;
            _init = init;

            Session.OnAudioDataArrived += Session_OnAudioDataArrived;
            Session.OnAudioStreamComplete += Session_OnAudioStreamComplete;
            Session.AudioBufferStats += Session_AudioBufferStats;

            sp_availability avail = sp_track_get_availability(Session.SessionPtr, _trackPtr);

            if (avail != sp_availability.SP_TRACK_AVAILABILITY_AVAILABLE)
            {
                LogTo.Warn("Track is unavailable: {0}", avail);
                return;
            }

            // TODO if time is in the future, block here
            // TODO if time is more than a few ms in the future, prefetch song

            Session.Play();
            if (startTime != DateTime.MinValue)
                Session.Seek((int) (DateTime.Now - startTime).TotalMilliseconds);
        }

        public void StopStreaming()
        {
            if (_loaded)
            {
                Session.OnAudioDataArrived -= Session_OnAudioDataArrived;
                Session.OnAudioStreamComplete -= Session_OnAudioStreamComplete;
                Session.AudioBufferStats -= Session_AudioBufferStats;

                Session.UnloadPlayer();
                _loaded = false;
            }
        }

        private int jitter;

        private void Session_AudioBufferStats(ref sp_audio_buffer_stats obj)
        {
            if (_wave == null)
                return;

            obj.samples = _wave.BufferedBytes / 2;
            obj.stutter = jitter;
            jitter = 0;
        }

        private void Session_OnAudioStreamComplete(object obj)
        {
            _complete = true;
        }

        private void Session_OnAudioDataArrived(byte[] buffer, sp_audioformat format)
        {
            if ((waveFormat == null) || (format.channels != waveFormat.Channels) || (format.sample_rate != waveFormat.SampleRate))
            {
                SetAudioFormat(format);
            }

            if (!_complete && _loaded)
            {
                // Try to keep buffer mostly full
                if (_wave.BufferedBytes < _wave.BufferLength - 40000)
                    jitter++;

                _wave.AddSamples(buffer, 0, buffer.Length);
            }
        }

        private void SetAudioFormat(sp_audioformat format)
        {
            waveFormat = new WaveFormat(format.sample_rate, 16, format.channels);

            _wave = new BufferedWaveProvider(waveFormat);
            _wave.BufferDuration = TimeSpan.FromSeconds(15);

            _init(_wave);
        }
    }
}