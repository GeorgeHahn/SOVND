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

        public enum sp_linktype {
            SP_LINKTYPE_INVALID = 0,
            SP_LINKTYPE_TRACK = 1,
            SP_LINKTYPE_ALBUM = 2,
            SP_LINKTYPE_ARTIST = 3,
            SP_LINKTYPE_SEARCH = 4,
            SP_LINKTYPE_PLAYLIST = 5,
            SP_LINKTYPE_PROFILE = 6,
            SP_LINKTYPE_STARRED = 7,
            SP_LINKTYPE_LOCALTRACK = 8,
            SP_LINKTYPE_IMAGE = 9
        }

        [DllImport("libspotify")]
        public static extern IntPtr sp_link_create_from_string(IntPtr linkString);

        [DllImport("libspotify")]
        public static extern IntPtr sp_link_create_from_track(IntPtr trackPtr, int offset);

        [DllImport("libspotify")]
        public static extern IntPtr sp_link_create_from_album(IntPtr albumPtr);

        [DllImport("libspotify")]
        public static extern IntPtr sp_link_create_from_album_cover(IntPtr albumPtr, sp_image_size size);

        [DllImport("libspotify")]
        public static extern IntPtr sp_link_create_from_artist(IntPtr artistPtr);

        [DllImport("libspotify")]
        public static extern IntPtr sp_link_create_from_artist_portrait(IntPtr artistPtr);

        [DllImport("libspotify")]
        public static extern IntPtr sp_link_create_from_artistbrowse_portrait(IntPtr artistBrowsePtr, int index);

        [DllImport("libspotify")]
        public static extern IntPtr sp_link_create_from_search(IntPtr searchPtr);

        [DllImport("libspotify")]
        public static extern IntPtr sp_link_create_from_playlist(IntPtr playlistPtr);

        [DllImport("libspotify")]
        public static extern IntPtr sp_link_create_from_user(IntPtr userPtr);

        [DllImport("libspotify")]
        public static extern int sp_link_as_string(IntPtr linkPtr, IntPtr bufferPtr, int buffer_size);

        [DllImport("libspotify")]
        public static extern sp_linktype sp_link_type(IntPtr linkPtr);

        [DllImport("libspotify")]
        public static extern IntPtr sp_link_as_track(IntPtr linkPtr);

        [DllImport("libspotify")]
        public static extern IntPtr sp_link_as_track_and_offset(IntPtr linkPtr, out IntPtr offsetPtr);

        [DllImport("libspotify")]
        public static extern IntPtr sp_link_as_album(IntPtr linkPtr);

        [DllImport("libspotify")]
        public static extern IntPtr sp_link_as_artist(IntPtr linkPtr);

        [DllImport("libspotify")]
        public static extern IntPtr sp_link_as_user(IntPtr linkPtr);

        [DllImport("libspotify")]
        public static extern sp_error sp_link_add_ref(IntPtr linkPtr);

        [DllImport("libspotify")]
        public static extern sp_error sp_link_release(IntPtr linkPtr);

    }

}
