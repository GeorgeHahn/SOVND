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
using System.Threading;
using libspotifydotnet;
using NAudio.Wave;
using System.Threading.Tasks;
using NAudio.Utils;

namespace SpotifyClient
{
    public class SpotifyTrackDataPipe
    {
        private IntPtr _trackPtr;
        private Action<byte[]> d_OnAudioDataArrived = null;
        private Action<object> d_OnAudioStreamComplete = null;
        private Queue<byte[]> _q = new Queue<byte[]>();
        private bool _complete;

        private static bool _interrupt;
        private static bool _loaded;
        private static object _syncObj = new object();

        private BufferedWaveProvider _wave;

        public SpotifyTrackDataPipe(IntPtr trackPtr, BufferedWaveProvider wave)
        {
            _trackPtr = trackPtr;
            _wave = wave;

            d_OnAudioDataArrived = new Action<byte[]>(Session_OnAudioDataArrived);
            d_OnAudioStreamComplete = new Action<object>(Session_OnAudioStreamComplete);

            Session.OnAudioDataArrived += d_OnAudioDataArrived;
            Session.OnAudioStreamComplete += d_OnAudioStreamComplete;
            Session.AudioBufferStats += Session_AudioBufferStats;
            StartStreaming();
        }

        public bool Complete { get { return _complete; } }

        public void StartStreaming()
        {
            _interrupt = true;
                
            lock (_syncObj)
            {
                _interrupt = false;

                if (_loaded)
                {
                    Session.UnloadPlayer();
                    _loaded = false;
                }

                _loaded = true;
                var error = Session.LoadPlayer(_trackPtr);

                if (error != libspotify.sp_error.OK)
                    throw new Exception("[Spotify] \{libspotify.sp_error_message(error)}");

                libspotify.sp_availability avail = libspotify.sp_track_get_availability(Session.SessionPtr, _trackPtr);
            
                if (avail != libspotify.sp_availability.SP_TRACK_AVAILABILITY_AVAILABLE)
                    throw new Exception("Track is unavailable (\{avail}).");

                Session.Play();
            }
        }

        public void StopStreaming()
        {
            if (_loaded)
            {
                Session.UnloadPlayer();
                _loaded = false;
            }
        }

        private int jitter = 0;

        private void Session_AudioBufferStats(ref libspotify.sp_audio_buffer_stats obj)
        {
            obj.samples = _wave.BufferedBytes;
            obj.stutter = jitter;
            jitter = 0;
        }

        private void Session_OnAudioStreamComplete(object obj)
        {
            _complete = true;
        }

        private void Session_OnAudioDataArrived(byte[] buffer)
        {
            if (!_interrupt && !_complete)
            {
                // Try to keep buffer 1/2 full
                if (_wave.BufferedDuration.TotalSeconds < _wave.BufferDuration.TotalSeconds / 2)
                    jitter++;

                _wave.AddSamples(buffer, 0, buffer.Length);
            }
        }
    }
}