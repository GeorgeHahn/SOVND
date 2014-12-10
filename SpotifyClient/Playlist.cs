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
using System.Runtime.InteropServices;
using Anotar.NLog;
using libspotifydotnet.libspotify;

namespace SpotifyClient
{
    public class Playlist : IDisposable
    {
        private static string LOG_MODULE = "Playlist";

        private delegate void tracks_added_delegate(IntPtr playlistPtr, IntPtr tracksPtr, int num_tracks, int position, IntPtr userDataPtr);

        private delegate void tracks_removed_delegate(IntPtr playlistPtr, IntPtr tracksPtr, int num_tracks, IntPtr userDataPtr);

        private delegate void tracks_moved_delegate(IntPtr playlistPtr, IntPtr tracksPtr, int num_tracks, int new_position, IntPtr userDataPtr);

        private delegate void playlist_renamed_delegate(IntPtr playlistPtr, IntPtr userDataPtr);

        private delegate void playlist_state_changed_delegate(IntPtr playlistPtr, IntPtr userDataPtr);

        private delegate void playlist_update_in_progress_delegate(IntPtr playlistPtr, bool done, IntPtr userDataPtr);

        private delegate void playlist_metadata_updated_delegate(IntPtr playlistPtr, IntPtr userDataPtr);

        private delegate void track_created_changed_delegate(IntPtr playlistPtr, int position, IntPtr userPtr, int when, IntPtr userDataPtr);

        private delegate void track_seen_changed_delegate(IntPtr playlistPtr, int position, bool seen, IntPtr userDataPtr);

        private delegate void description_changed_delegate(IntPtr playlistPtr, IntPtr descPtr, IntPtr userDataPtr);

        private delegate void image_changed_delegate(IntPtr playlistPtr, IntPtr imagePtr, IntPtr userDataPtr);

        private delegate void track_message_changed_delegate(IntPtr playlistPtr, int position, IntPtr messagePtr, IntPtr userDataPtr);

        private delegate void subscribers_changed_delegate(IntPtr playlistPtr, IntPtr userDataPtr);

        private tracks_added_delegate fn_tracks_added;
        private tracks_removed_delegate fn_tracks_removed;
        private tracks_moved_delegate fn_tracks_moved;
        private playlist_renamed_delegate fn_playlist_renamed;
        private playlist_state_changed_delegate fn_playlist_state_changed;
        private playlist_update_in_progress_delegate fn_playlist_update_in_progress;
        private playlist_metadata_updated_delegate fn_playlist_metadata_updated;
        private track_created_changed_delegate fn_track_created_changed;
        private track_seen_changed_delegate fn_track_seen_changed;
        private description_changed_delegate fn_description_changed;
        private image_changed_delegate fn_image_changed;
        private track_message_changed_delegate fn_track_message_changed;
        private subscribers_changed_delegate fn_subscribers_changed;

        private IntPtr _callbacksPtr;
        private bool _disposed;

        public string Name { get; private set; }

        public int TrackCount { get; private set; }

        public string Description { get; private set; }

        public int SubscriberCount { get; private set; }

        public bool IsInRAM { get; private set; }

        public sp_playlist_offline_status OfflineStatus { get; private set; }

        public IntPtr Pointer { get; private set; }

        private List<Track> _tracks;

        #region IDisposable Members

        public void Dispose()
        {
            dispose(true);
            GC.SuppressFinalize(this);
        }

        ~Playlist()
        {
            dispose(false);
        }

        private void dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    safeRemoveCallbacks();
                }

                _disposed = true;
            }
        }

        #endregion IDisposable Members

        public static Playlist FromLink(string playlistLink)
        {
            IntPtr linkPtr = Functions.StringToLinkPtr(playlistLink);
            try
            {
                return new Playlist(sp_playlist_create(Session.SessionPtr, linkPtr));
            }
            finally
            {
                sp_link_release(linkPtr);
            }
        }

        public static string GetLink(IntPtr playlistPtr)
        {
            IntPtr linkPtr = sp_link_create_from_playlist(playlistPtr);
            return Functions.LinkPtrToString(linkPtr);
        }

        public Playlist(IntPtr playlistPtr)
        {
            this.Pointer = playlistPtr;

            initCallbacks();

            if (this.IsLoaded)
            {
                populateMetadata();                
            }
        }

        public bool IsLoaded
        {
            get { return this.Pointer != IntPtr.Zero && sp_playlist_is_loaded(this.Pointer); }
        }

        public bool TracksAreLoaded
        {
            get
            {
                if (this.Pointer == IntPtr.Zero
                    || !this.IsLoaded)
                    return false;

                for (int i = 0; i < this.TrackCount; i++)
                {
                    IntPtr trackPtr = sp_playlist_track(this.Pointer, i);
                    if (!sp_track_is_loaded(trackPtr))
                        return false;
                }

                return true;
            }
        }

        private void initCallbacks()
        {
            if (this.Pointer == IntPtr.Zero)
                throw new InvalidOperationException("Invalid playlist pointer.");

            this.fn_tracks_added = new tracks_added_delegate(tracks_added);
            this.fn_tracks_removed = new tracks_removed_delegate(tracks_removed);
            this.fn_tracks_moved = new tracks_moved_delegate(tracks_moved);
            this.fn_playlist_renamed = new playlist_renamed_delegate(playlist_renamed);
            this.fn_playlist_state_changed = new playlist_state_changed_delegate(state_changed);
            this.fn_playlist_update_in_progress = new playlist_update_in_progress_delegate(playlist_update_in_progress);
            this.fn_playlist_metadata_updated = new playlist_metadata_updated_delegate(metadata_updated);
            this.fn_track_created_changed = new track_created_changed_delegate(track_created_changed);
            this.fn_track_seen_changed = new track_seen_changed_delegate(track_seen_changed);
            this.fn_description_changed = new description_changed_delegate(description_changed);
            this.fn_image_changed = new image_changed_delegate(image_changed);
            this.fn_track_message_changed = new track_message_changed_delegate(track_message_changed);
            this.fn_subscribers_changed = new subscribers_changed_delegate(subscribers_changed);

            sp_playlist_callbacks callbacks = new sp_playlist_callbacks();

            callbacks.tracks_added = Marshal.GetFunctionPointerForDelegate(fn_tracks_added);
            callbacks.tracks_removed = Marshal.GetFunctionPointerForDelegate(fn_tracks_removed);
            callbacks.tracks_moved = Marshal.GetFunctionPointerForDelegate(fn_tracks_moved);
            callbacks.playlist_renamed = Marshal.GetFunctionPointerForDelegate(fn_playlist_renamed);
            callbacks.playlist_state_changed = Marshal.GetFunctionPointerForDelegate(fn_playlist_state_changed);
            callbacks.playlist_update_in_progress = Marshal.GetFunctionPointerForDelegate(fn_playlist_update_in_progress);
            callbacks.playlist_metadata_updated = Marshal.GetFunctionPointerForDelegate(fn_playlist_metadata_updated);
            callbacks.track_created_changed = Marshal.GetFunctionPointerForDelegate(fn_track_created_changed);
            callbacks.track_seen_changed = Marshal.GetFunctionPointerForDelegate(fn_track_seen_changed);
            callbacks.description_changed = Marshal.GetFunctionPointerForDelegate(fn_description_changed);
            callbacks.image_changed = Marshal.GetFunctionPointerForDelegate(fn_image_changed);
            callbacks.track_message_changed = Marshal.GetFunctionPointerForDelegate(fn_track_message_changed);
            callbacks.subscribers_changed = Marshal.GetFunctionPointerForDelegate(fn_subscribers_changed);

            _callbacksPtr = Marshal.AllocHGlobal(Marshal.SizeOf(callbacks));
            Marshal.StructureToPtr(callbacks, _callbacksPtr, true);

            sp_playlist_add_callbacks(this.Pointer, _callbacksPtr, IntPtr.Zero);
        }

        private void safeRemoveCallbacks()
        {
            try
            {
                if (this.Pointer == IntPtr.Zero)
                    return;

                if (_callbacksPtr == IntPtr.Zero)
                    return;

                sp_playlist_remove_callbacks(this.Pointer, _callbacksPtr, IntPtr.Zero);
            }
            catch { }
        }

        public List<Track> GetTracks()
        {
            if (_tracks == null)
            {
                _tracks = new List<Track>();

                for (int i = 0; i < this.TrackCount; i++)
                {
                    IntPtr trackPtr = sp_playlist_track(this.Pointer, i);

                    _tracks.Add(new Track(trackPtr));
                }
            }

            return _tracks;
        }

        private void populateMetadata()
        {
            this.Name = Functions.PtrToString(sp_playlist_name(this.Pointer));
            this.TrackCount = sp_playlist_num_tracks(this.Pointer);
            this.Description = Functions.PtrToString(sp_playlist_get_description(this.Pointer));
            this.SubscriberCount = (int)sp_playlist_num_subscribers(this.Pointer);
            this.IsInRAM = sp_playlist_is_in_ram(Session.SessionPtr, this.Pointer);
            this.OfflineStatus = sp_playlist_get_offline_status(Session.SessionPtr, this.Pointer);
            this.TrackCount = sp_playlist_num_tracks(this.Pointer);
        }

        private void tracks_added(IntPtr playlistPtr, IntPtr tracksPtr, int num_tracks, int position, IntPtr userDataPtr)
        {
            Log.Trace(LOG_MODULE, "tracks_added num_tracks={0}, position={1}", num_tracks, position);
        }

        private void tracks_removed(IntPtr playlistPtr, IntPtr tracksPtr, int num_tracks, IntPtr userDataPtr)
        {
            Log.Trace(LOG_MODULE, "tracks_removed num_tracks={0}", num_tracks);
        }

        private void tracks_moved(IntPtr playlistPtr, IntPtr tracksPtr, int num_tracks, int new_position, IntPtr userDataPtr)
        {
            Log.Trace(LOG_MODULE, "tracks_moved num_tracks={0}, new_position={1}", num_tracks, new_position);
        }

        private void playlist_renamed(IntPtr playlistPtr, IntPtr userDataPtr)
        {
            Log.Trace(LOG_MODULE, "playlist_renamed");
            populateMetadata();
        }

        private void state_changed(IntPtr playlistPtr, IntPtr userDataPtr)
        {
            Log.Trace(LOG_MODULE, "state_changed");
            if (sp_playlist_is_loaded(playlistPtr))
            {
                populateMetadata();
            }
        }

        private void playlist_update_in_progress(IntPtr playlistPtr, bool done, IntPtr userDataPtr)
        {
            Log.Trace(LOG_MODULE, "playlist_update_in_progress done={0}", done);
        }

        private void metadata_updated(IntPtr playlistPtr, IntPtr userDataPtr)
        {
            LogTo.Trace("Playlist metadata updated");
            Log.Trace(LOG_MODULE, "metadata_updated");
            populateMetadata();
        }

        private void track_created_changed(IntPtr playlistPtr, int position, IntPtr userPtr, int when, IntPtr userDataPtr)
        {
            Log.Trace(LOG_MODULE, "track_created_changed position={0}", position);
        }

        private void track_seen_changed(IntPtr playlistPtr, int position, bool seen, IntPtr userDataPtr)
        {
            Log.Trace(LOG_MODULE, "track_seen_changed position={0}, seen={1}", position, seen);
        }

        private void description_changed(IntPtr playlistPtr, IntPtr descPtr, IntPtr userDataPtr)
        {
            Log.Trace(LOG_MODULE, "description_changed");
        }

        private void image_changed(IntPtr playlistPtr, IntPtr imagePtr, IntPtr userDataPtr)
        {
            Log.Trace(LOG_MODULE, "image_changed");
        }

        private void track_message_changed(IntPtr playlistPtr, int position, IntPtr messagePtr, IntPtr userDataPtr)
        {
            Log.Trace(LOG_MODULE, "track_message_changed position={0}", position);
        }

        private void subscribers_changed(IntPtr playlistPtr, IntPtr userDataPtr)
        {
            Log.Trace(LOG_MODULE, "subscribers_changed");
        }
    }
}