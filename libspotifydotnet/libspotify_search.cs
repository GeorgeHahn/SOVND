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

    public delegate void search_complete_cb_delegate(IntPtr searchPtr, IntPtr userDataPtr);

    public enum sp_search_type {
        SP_SEARCH_STANDARD = 0,
        SP_SEARCH_SUGGEST = 1,
    }

    public static partial class libspotify {
        
        [DllImport("libspotify")]
        public static extern IntPtr sp_search_create(IntPtr sessionPtr, IntPtr query, int track_offset, int track_count,
                                                       int album_offset, int album_count, int artist_offset, int artist_count,
                                                       int playlist_offset, int playlist_count, sp_search_type search_type,
                                                       IntPtr callbackPtr, IntPtr userDataPtr);
        [DllImport("libspotify")]
        public static extern bool sp_search_is_loaded(IntPtr searchPtr);

        [DllImport("libspotify")]
        public static extern sp_error sp_search_error(IntPtr searchPtr);

        [DllImport("libspotify")]
        public static extern int sp_search_num_tracks(IntPtr searchPtr);

        [DllImport("libspotify")]
        public static extern IntPtr sp_search_track(IntPtr searchPtr, int index);

        [DllImport("libspotify")]
        public static extern int sp_search_num_albums(IntPtr searchPtr);

        [DllImport("libspotify")]
        public static extern IntPtr sp_search_album(IntPtr searchPtr, int index);

        [DllImport("libspotify")]
        public static extern int sp_search_num_artists(IntPtr searchPtr);

        [DllImport("libspotify")]
        public static extern IntPtr sp_search_artist(IntPtr searchPtr, int index);

        [DllImport("libspotify")]
        public static extern IntPtr sp_search_query(IntPtr searchPtr);

        [DllImport("libspotify")]
        public static extern IntPtr sp_search_did_you_mean(IntPtr searchPtr);

        [DllImport("libspotify")]
        public static extern int sp_search_total_tracks(IntPtr searchPtr);

        [DllImport("libspotify")]
        public static extern int sp_search_total_albums(IntPtr searchPtr);

        [DllImport("libspotify")]
        public static extern int sp_search_total_artists(IntPtr searchPtr);

        [DllImport("libspotify")]
        public static extern sp_error sp_search_add_ref(IntPtr searchPtr);

        [DllImport("libspotify")]
        public static extern sp_error sp_search_release(IntPtr searchPtr);

        [DllImport("libspotify")]
        public static extern int sp_search_num_playlists(IntPtr searchPtr);

        [DllImport("libspotify")]
        public static extern int sp_search_total_playlists(IntPtr searchPtr);

        [DllImport("libspotify")]
        public static extern IntPtr sp_search_playlist(IntPtr searchPtr, int index);

        [DllImport("libspotify")]
        public static extern IntPtr sp_search_playlist_name(IntPtr searchPtr, int index);

        [DllImport("libspotify")]
        public static extern IntPtr sp_search_playlist_uri(IntPtr searchPtr, int index);

        [DllImport("libspotify")]
        public static extern IntPtr sp_search_playlist_image_uri(IntPtr searchPtr, int index);
    }

}
