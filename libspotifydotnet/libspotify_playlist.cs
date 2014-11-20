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
using System.Runtime.InteropServices;

namespace libspotifydotnet {

    public static partial class libspotify {

        public struct sp_playlist_callbacks {
            public IntPtr tracks_added;
            public IntPtr tracks_removed;
            public IntPtr tracks_moved;
            public IntPtr playlist_renamed;
            public IntPtr playlist_state_changed;
            public IntPtr playlist_update_in_progress;
            public IntPtr playlist_metadata_updated;
            public IntPtr track_created_changed;
            public IntPtr track_seen_changed;
            public IntPtr description_changed;
            public IntPtr image_changed;
            public IntPtr track_message_changed;
            public IntPtr subscribers_changed;
        }

        public struct sp_playlistcontainer_callbacks {
            public IntPtr playlist_added;
            public IntPtr playlist_removed;
            public IntPtr playlist_moved;
            public IntPtr container_loaded;
        }

        [DllImport("libspotify")]
        public static extern bool sp_playlist_is_loaded(IntPtr playlistPtr);

        [DllImport("libspotify")]
        public static extern sp_error sp_playlist_add_callbacks(IntPtr playlistPtr, IntPtr callbacksPtr, IntPtr userDataPtr);

        [DllImport("libspotify")]
        public static extern sp_error sp_playlist_remove_callbacks(IntPtr playlistPtr, IntPtr callbacksPtr, IntPtr userDataPtr);

        [DllImport("libspotify")]
        public static extern int sp_playlist_num_tracks(IntPtr playlistPtr);

        [DllImport("libspotify")]
        public static extern IntPtr sp_playlist_track(IntPtr playlistPtr, int index);

        [DllImport("libspotify")]
        public static extern int sp_playlist_track_create_time(IntPtr playlistPtr, int index);

        [DllImport("libspotify")]
        public static extern IntPtr sp_playlist_track_creator(IntPtr playlistPtr, int index);

        [DllImport("libspotify")]
        public static extern bool sp_playlist_track_seen(IntPtr playlistPtr, int index);

        [DllImport("libspotify")]
        public static extern sp_error sp_playlist_track_set_seen(IntPtr playlistPtr, int index, bool seen);

        [DllImport("libspotify")]
        public static extern IntPtr sp_playlist_track_message(IntPtr playlistPtr, int index);

        [DllImport("libspotify")]
        public static extern IntPtr sp_playlist_name(IntPtr playlistPtr);

        [DllImport("libspotify")]
        public static extern sp_error sp_playlist_rename(IntPtr playlistPtr, IntPtr newNamePtr);

        [DllImport("libspotify")]
        public static extern IntPtr sp_playlist_owner(IntPtr playlistPtr);

        [DllImport("libspotify")]
        public static extern bool sp_playlist_is_collaborative(IntPtr playlistPtr);

        [DllImport("libspotify")]
        public static extern sp_error sp_playlist_set_collaborative(IntPtr playlistPtr, bool collaborative);

        [DllImport("libspotify")]
        public static extern sp_error sp_playlist_set_autolink_tracks(IntPtr playlistPtr, bool link);

        [DllImport("libspotify")]
        public static extern IntPtr sp_playlist_get_description(IntPtr playlistPtr);

        [DllImport("libspotify")]
        public static extern bool sp_playlist_get_image(IntPtr playlistPtr, IntPtr imagePtr);

        [DllImport("libspotify")]
        public static extern bool sp_playlist_has_pending_changes(IntPtr playlistPtr);

        [DllImport("libspotify")]
        public static extern sp_error sp_playlist_add_tracks(IntPtr playlistPtr, IntPtr tracksArrayPtr, int num_tracks, int position, IntPtr sessionPtr);

        [DllImport("libspotify")]
        public static extern sp_error sp_playlist_remove_tracks(IntPtr playlistPtr, int[] trackIndicies, int num_tracks);

        [DllImport("libspotify")]
        public static extern sp_error sp_playlist_reorder_tracks(IntPtr playlistPtr, int[] trackIndicies, int num_tracks, int new_position);

        [DllImport("libspotify")]
        public static extern uint sp_playlist_num_subscribers(IntPtr playlistPtr);

        [DllImport("libspotify")]
        public static extern IntPtr sp_playlist_subscribers(IntPtr playlistPtr);

        [DllImport("libspotify")]
        public static extern bool sp_playlist_is_in_ram(IntPtr sessionPtr, IntPtr playlistPtr);

        [DllImport("libspotify")]
        public static extern IntPtr sp_playlist_create(IntPtr sessionPtr, IntPtr linkPtr);

        [DllImport("libspotify")]
        public static extern sp_error sp_playlist_set_offine_mode(IntPtr sessionPtr, IntPtr playlistPtr, bool offline);

        [DllImport("libspotify")]
        public static extern sp_playlist_offline_status sp_playlist_get_offline_status(IntPtr sessionPtr, IntPtr playlistPtr);

        [DllImport("libspotify")]
        public static extern int sp_playlist_get_offline_download_completed(IntPtr sessionPtr, IntPtr playlistPtr);

        [DllImport("libspotify")]
        public static extern sp_error sp_playlist_add_ref(IntPtr playlistPtr);

        [DllImport("libspotify")]
        public static extern sp_error sp_playlist_release(IntPtr playlistPtr);     

        [DllImport("libspotify")]
        public static extern int sp_playlistcontainer_add_callbacks(IntPtr playlistContainerPtr, IntPtr ptrCallbacks, IntPtr userdata);

        [DllImport("libspotify")]
        public static extern int sp_playlistcontainer_remove_callbacks(IntPtr playlistContainerPtr, IntPtr ptrCallbacks, IntPtr userdata);

        [DllImport("libspotify")]
        public static extern int sp_playlistcontainer_num_playlists(IntPtr playlistContainerPtr);

        [DllImport("libspotify")]
        public static extern bool sp_playlistcontainer_is_loaded(IntPtr playlistContainerPtr);

        [DllImport("libspotify")]
        public static extern IntPtr sp_playlistcontainer_playlist(IntPtr playlistContainerPtr, int index);

        [DllImport("libspotify")]
        public static extern sp_playlist_type sp_playlistcontainer_playlist_type(IntPtr playlistContainerPtr, int index);

        [DllImport("libspotify")]
        public static extern sp_error sp_playlistcontainer_playlist_folder_name(IntPtr playlistContainerPtr, int index, IntPtr buffer, int buffer_size);

        [DllImport("libspotify")]
        public static extern ulong sp_playlistcontainer_playlist_folder_id(IntPtr playlistContainerPtr, int index);

        [DllImport("libspotify")]
        public static extern IntPtr sp_playlistcontainer_add_new_playlist(IntPtr playlistContainerPtr, IntPtr namePtr);

        [DllImport("libspotify")]
        public static extern IntPtr sp_playlistcontainer_add_playlist(IntPtr playlistContainerPtr, IntPtr linkPtr);

        [DllImport("libspotify")]
        public static extern sp_error sp_playlistcontainer_remove_playlist(IntPtr playlistContainerPtr, int index);

        [DllImport("libspotify")]
        public static extern sp_error sp_playlistcontainer_move_playlist(IntPtr playlistContainerPtr, int index, int new_position, bool dry_run);

        [DllImport("libspotify")]
        public static extern sp_error sp_playlistcontainer_add_folder(IntPtr playlistContainerPtr, int index, IntPtr namePtr);

        [DllImport("libspotify")]
        public static extern IntPtr sp_playlistcontainer_owner(IntPtr playlistContainerPtr);

        [DllImport("libspotify")]
        public static extern sp_error sp_playlistcontainer_release(IntPtr playlistContainerPtr);
        
        [DllImport("libspotify")]
        public static extern int sp_playlistcontainer_get_unseen_tracks(IntPtr playlistContainerPtr, IntPtr playlistPtr, IntPtr tracks, int num_tracks);

        [DllImport("libspotify")]
        public static extern int sp_playlistcontainer_clear_unseen_tracks(IntPtr playlistContainerPtr, IntPtr playlistPtr);

    }

}
