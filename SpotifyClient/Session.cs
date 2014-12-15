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
using System.Environment;
using System.IO;
using System.Runtime.InteropServices;
using Anotar.NLog;
using libspotifydotnet.libspotify;

namespace SpotifyClient
{
    public static class Session
    {
        private static IntPtr _sessionPtr;

        private delegate void connection_error_delegate(IntPtr sessionPtr, sp_error error);

        private delegate void end_of_track_delegate(IntPtr sessionPtr);

        private delegate void get_audio_buffer_stats_delegate(IntPtr sessionPtr, ref sp_audio_buffer_stats statsPtr);
        public delegate void get_audiobufferstats(ref sp_audio_buffer_stats statsPtr);

        private delegate void log_message_delegate(IntPtr sessionPtr, string message);

        private delegate void logged_in_delegate(IntPtr sessionPtr, sp_error error);

        private delegate void logged_out_delegate(IntPtr sessionPtr);

        private delegate void message_to_user_delegate(IntPtr sessionPtr, string message);

        private delegate void metadata_updated_delegate(IntPtr sessionPtr);

        private delegate int music_delivery_delegate(IntPtr sessionPtr, IntPtr formatPtr, IntPtr framesPtr, int num_frames);

        private delegate void notify_main_thread_delegate(IntPtr sessionPtr);

        private delegate void offline_status_updated_delegate(IntPtr sessionPtr);

        private delegate void play_token_lost_delegate(IntPtr sessionPtr);

        private delegate void start_playback_delegate(IntPtr sessionPtr);

        private delegate void stop_playback_delegate(IntPtr sessionPtr);

        private delegate void streaming_error_delegate(IntPtr sessionPtr, sp_error error);

        private delegate void userinfo_updated_delegate(IntPtr sessionPtr);

        private static connection_error_delegate fn_connection_error_delegate = new connection_error_delegate(connection_error);
        private static end_of_track_delegate fn_end_of_track_delegate = new end_of_track_delegate(end_of_track);
        private static get_audio_buffer_stats_delegate fn_get_audio_buffer_stats_delegate = new get_audio_buffer_stats_delegate(get_audio_buffer_stats);
        private static log_message_delegate fn_log_message = new log_message_delegate(log_message);
        private static logged_in_delegate fn_logged_in_delegate = new logged_in_delegate(logged_in);
        private static logged_out_delegate fn_logged_out_delegate = new logged_out_delegate(logged_out);
        private static message_to_user_delegate fn_message_to_user_delegate = new message_to_user_delegate(message_to_user);
        private static metadata_updated_delegate fn_metadata_updated_delegate = new metadata_updated_delegate(metadata_updated);
        private static music_delivery_delegate fn_music_delivery_delegate = new music_delivery_delegate(music_delivery);
        private static notify_main_thread_delegate fn_notify_main_thread_delegate = new notify_main_thread_delegate(notify_main_thread);
        private static offline_status_updated_delegate fn_offline_status_updated_delegate = new offline_status_updated_delegate(offline_status_updated);
        private static play_token_lost_delegate fn_play_token_lost_delegate = new play_token_lost_delegate(play_token_lost);
        private static start_playback_delegate fn_start_playback = new start_playback_delegate(start_playback);
        private static stop_playback_delegate fn_stop_playback = new stop_playback_delegate(stop_playback);
        private static streaming_error_delegate fn_streaming_error_delegate = new streaming_error_delegate(streaming_error);
        private static userinfo_updated_delegate fn_userinfo_updated_delegate = new userinfo_updated_delegate(userinfo_updated);

        private static byte[] appkey = null;
        private static sp_error _loginError = sp_error.OK;
        private static bool _isLoggedIn = false;

        public static event Action<IntPtr> OnNotifyMainThread;

        public static event Action<IntPtr> OnLoggedIn;

        public static Func<byte[], sp_audioformat, int> OnAudioDataArrived;

        public static Action<object> OnAudioStreamComplete;

        public static get_audiobufferstats AudioBufferStats;

        public static IntPtr SessionPtr
        {
            get { return _sessionPtr; }
        }

        public static sp_error LoginError
        {
            get { return _loginError; }
        }

        public static bool IsLoggedIn
        {
            get { return _isLoggedIn; }
        }

        public static void Login(object[] args)
        {
            appkey = (byte[])args[0];

            if (_sessionPtr == IntPtr.Zero)
                _loginError = initSession((string)args[3]);

            if (_loginError != sp_error.OK)
                throw new ApplicationException(Functions.PtrToString(sp_error_message(_loginError)));

            if (_sessionPtr == IntPtr.Zero)
                throw new InvalidOperationException("Session initialization failed, session pointer is null.");

            sp_session_login(_sessionPtr, args[1].ToString(), args[2].ToString(), false, null);
        }

        public static void Logout()
        {
            if (_sessionPtr == IntPtr.Zero)
                return;

            sp_session_logout(_sessionPtr);
            _sessionPtr = IntPtr.Zero;
        }

        public static int GetUserCountry()
        {
            if (_sessionPtr == IntPtr.Zero)
                throw new InvalidOperationException("No session.");

            return sp_session_user_country(_sessionPtr);
        }

        public static sp_error LoadPlayer(IntPtr trackPtr)
        {
            return sp_session_player_load(_sessionPtr, trackPtr);
        }

        public static sp_error PrefetchTrack(IntPtr trackPtr)
        {
            return sp_session_player_prefetch(_sessionPtr, trackPtr);
        }

        public static void Play()
        {
            sp_session_player_play(_sessionPtr, true);
        }

        public static void Seek(int ms)
        {
            sp_session_player_seek(_sessionPtr, ms);
        }

        public static void Pause()
        {
            sp_session_player_play(_sessionPtr, false);
        }

        public static void UnloadPlayer()
        {
            sp_session_player_unload(_sessionPtr);
        }

        private static sp_error initSession(string appname)
        {
            sp_session_callbacks callbacks = new sp_session_callbacks();
            callbacks.connection_error = Marshal.GetFunctionPointerForDelegate(fn_connection_error_delegate);
            callbacks.end_of_track = Marshal.GetFunctionPointerForDelegate(fn_end_of_track_delegate);
            callbacks.get_audio_buffer_stats = Marshal.GetFunctionPointerForDelegate(fn_get_audio_buffer_stats_delegate);
            callbacks.log_message = Marshal.GetFunctionPointerForDelegate(fn_log_message);
            callbacks.logged_in = Marshal.GetFunctionPointerForDelegate(fn_logged_in_delegate);
            callbacks.logged_out = Marshal.GetFunctionPointerForDelegate(fn_logged_out_delegate);
            callbacks.message_to_user = Marshal.GetFunctionPointerForDelegate(fn_message_to_user_delegate);
            callbacks.metadata_updated = Marshal.GetFunctionPointerForDelegate(fn_metadata_updated_delegate);
            callbacks.music_delivery = Marshal.GetFunctionPointerForDelegate(fn_music_delivery_delegate);
            callbacks.notify_main_thread = Marshal.GetFunctionPointerForDelegate(fn_notify_main_thread_delegate);
            callbacks.offline_status_updated = Marshal.GetFunctionPointerForDelegate(fn_offline_status_updated_delegate);
            callbacks.play_token_lost = Marshal.GetFunctionPointerForDelegate(fn_play_token_lost_delegate);
            callbacks.start_playback = Marshal.GetFunctionPointerForDelegate(fn_start_playback);
            callbacks.stop_playback = Marshal.GetFunctionPointerForDelegate(fn_stop_playback);
            callbacks.streaming_error = Marshal.GetFunctionPointerForDelegate(fn_streaming_error_delegate);
            callbacks.userinfo_updated = Marshal.GetFunctionPointerForDelegate(fn_userinfo_updated_delegate);

            IntPtr callbacksPtr = Marshal.AllocHGlobal(Marshal.SizeOf(callbacks));
            Marshal.StructureToPtr(callbacks, callbacksPtr, true);

            sp_session_config config = new sp_session_config();
            config.api_version = SPOTIFY_API_VERSION;
            config.user_agent = appname;
            config.application_key_size = appkey.Length;
            config.application_key = Marshal.AllocHGlobal(appkey.Length);
            config.cache_location = Path.Combine(GetFolderPath(SpecialFolder.ApplicationData), appname, "spotifycache");
            config.settings_location = Path.Combine(GetFolderPath(SpecialFolder.ApplicationData), appname, "spotifysettings");
            config.callbacks = callbacksPtr;
            config.compress_playlists = true;
            config.dont_save_metadata_for_playlists = false;
            config.initially_unload_playlists = false;

            Log.Debug(Plugin.LOG_MODULE, "api_version={0}", config.api_version);
            Log.Debug(Plugin.LOG_MODULE, "application_key_size={0}", config.application_key_size);
            Log.Debug(Plugin.LOG_MODULE, "cache_location={0}", config.cache_location);
            Log.Debug(Plugin.LOG_MODULE, "settings_location={0}", config.settings_location);

            Marshal.Copy(appkey, 0, config.application_key, appkey.Length);

            IntPtr sessionPtr;
            sp_error err = sp_session_create(ref config, out sessionPtr);
            sp_session_set_cache_size(sessionPtr, 0);

            if (err == sp_error.OK)
            {
                _sessionPtr = sessionPtr;
                sp_session_set_connection_type(sessionPtr, sp_connection_type.SP_CONNECTION_TYPE_WIRED);
            }

            sp_error test = sp_session_preferred_bitrate(sessionPtr, sp_bitrate.BITRATE_320k);
            if (test != sp_error.OK)
                Log.Warning(Plugin.LOG_MODULE, "sp_session_preferred_bitrate() failed: {0}", test);
            else
                Log.Debug(Plugin.LOG_MODULE, "sp_session_preferred_bitrate() succeeded!");

            return err;
        }

        private static void connection_error(IntPtr sessionPtr, sp_error error)
        {
            Log.Error(Plugin.LOG_MODULE, "Connection error: {0}", sp_error_message(error));
        }

        private static void end_of_track(IntPtr sessionPtr)
        {
            if (OnAudioStreamComplete != null)
                OnAudioStreamComplete(null);
        }

        private static void get_audio_buffer_stats(IntPtr sessionPtr, ref sp_audio_buffer_stats statsPtr)
        {
            if (AudioBufferStats != null)
                AudioBufferStats(ref statsPtr);
        }

        private static void log_message(IntPtr sessionPtr, string message)
        {
            if (message.EndsWith("\n"))
                message = message.Substring(0, message.Length - 1);

            Log.Debug("libspotify", "> " + message);
        }

        private static void logged_in(IntPtr sessionPtr, sp_error error)
        {
            if (error == sp_error.OK)
            {
                _isLoggedIn = true;
            }

            _loginError = error;

            if (OnLoggedIn != null)
                OnLoggedIn(sessionPtr);
        }

        private static void logged_out(IntPtr sessionPtr)
        {
            _isLoggedIn = false;
        }

        private static void message_to_user(IntPtr sessionPtr, string message)
        {
            LogTo.Info("Message to user: {0}", message);
        }

        private static void metadata_updated(IntPtr sessionPtr)
        {
            Track.Check();
        }

        private static int music_delivery(IntPtr sessionPtr, IntPtr formatPtr, IntPtr framesPtr, int num_frame)
        {
            // API 11 is firing this callback several times after the track ends.  num_frame is set to 22050,
            // which seems meaningful yet is way out of normal range (usually we get a couple hundred frames or less
            // at a time).  The buffers are all zeros, this adds a ton of extra silence to the end of the track for
            // no reason.  Docs don't talk about this new behavior, maybe related to gapless playback??
            // Workaround by ignoring any data received after the end_of_track callback; this ignore is done
            // in SpotifyTrackDataDataPipe.

            if((num_frame == 0) || (num_frame == 22050))
                LogTo.Trace("(\{sessionPtr.ToString()}, \{formatPtr.ToString()}, \{framesPtr.ToString()}, \{num_frame})");

            if (OnAudioDataArrived == null)
            {
                LogTo.Warn("OnAudioDataArrived is null");
                return 0;
            }

            sp_audioformat format = (sp_audioformat) Marshal.PtrToStructure(formatPtr, typeof (sp_audioformat));
            byte[] buffer = new byte[num_frame*sizeof (Int16)*format.channels];
            if(framesPtr != IntPtr.Zero)
                Marshal.Copy(framesPtr, buffer, 0, buffer.Length);
            return OnAudioDataArrived(buffer, format);
        }

        private static void notify_main_thread(IntPtr sessionPtr)
        {
            if (OnNotifyMainThread != null)
                OnNotifyMainThread(_sessionPtr);
        }

        private static void offline_status_updated(IntPtr sessionPtr)
        {
            Log.Debug(Plugin.LOG_MODULE, "offline_status_updated");
        }

        private static void play_token_lost(IntPtr sessionPtr)
        {
            Log.Debug(Plugin.LOG_MODULE, "play_token_lost");
        }

        private static void start_playback(IntPtr sessionPtr)
        {
            Log.Trace(Plugin.LOG_MODULE, "start_playback");
        }

        private static void stop_playback(IntPtr sessionPtr)
        {
            Log.Debug(Plugin.LOG_MODULE, "stop_playback");
        }

        private static void streaming_error(IntPtr sessionPtr, sp_error error)
        {
            Log.Error(Plugin.LOG_MODULE, "Streaming error: {0}", sp_error_message(error));
        }

        private static void userinfo_updated(IntPtr sessionPtr)
        {
            Log.Debug(Plugin.LOG_MODULE, "userinfo_updated");
        }
    }
}