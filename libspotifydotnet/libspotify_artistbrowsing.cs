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

        public enum sp_artistbrowse_type {
            [Obsolete("The SP_ARTISTBROWSE_FULL mode has been deprecated and will be removed in a future release.")]
            SP_ARTISTBROWSE_FULL = 0,
            SP_ARTISTBROWSE_NO_TRACKS = 1,
            SP_ARTISTBROWSE_NO_ALBUMS = 2
        }

        [DllImport("libspotify")]
        public static extern IntPtr sp_artistbrowse_create(IntPtr sessionPtr, IntPtr artistPtr, sp_artistbrowse_type type, IntPtr callbackPtr, IntPtr userDataPtr);

        [DllImport("libspotify")]
        public static extern bool sp_artistbrowse_is_loaded(IntPtr artistBrowsePtr);

        [DllImport("libspotify")]
        public static extern sp_error sp_artistbrowse_error(IntPtr artistBrowsePtr);

        [DllImport("libspotify")]
        public static extern IntPtr sp_artistbrowse_artist(IntPtr artistBrowsePtr);

        [DllImport("libspotify")]
        public static extern int sp_artistbrowse_num_portraits(IntPtr artistBrowsePtr);

        [DllImport("libspotify")]
        public static extern IntPtr sp_artistbrowse_portrait(IntPtr artistBrowsePtr, int index);

        [DllImport("libspotify")]
        public static extern int sp_artistbrowse_num_tracks(IntPtr artistBrowsePtr);

        [DllImport("libspotify")]
        public static extern IntPtr sp_artistbrowse_track(IntPtr artistBrowsePtr, int index);

        [DllImport("libspotify")]
        public static extern int sp_artistbrowse_tophit_tracks(IntPtr artistBrowsePtr);

        [DllImport("libspotify")]
        public static extern IntPtr sp_artistbrowse_tophit_track(IntPtr artistBrowsePtr, int index);

        [DllImport("libspotify")]
        public static extern int sp_artistbrowse_num_albums(IntPtr artistBrowsePtr);

        [DllImport("libspotify")]
        public static extern IntPtr sp_artistbrowse_album(IntPtr artistBrowsePtr, int index);

        [DllImport("libspotify")]
        public static extern int sp_artistbrowse_num_similar_artists(IntPtr artistBrowsePtr);

        [DllImport("libspotify")]
        public static extern IntPtr sp_artistbrowse_similar_artist(IntPtr artistBrowsePtr, int index);

        [DllImport("libspotify")]
        public static extern IntPtr sp_artistbrowse_biography(IntPtr artistBrowsePtr);

        [DllImport("libspotify")]
        public static extern int sp_artistbrowse_backend_request_duration(IntPtr artistBrowsePtr);

        [DllImport("libspotify")]
        public static extern sp_error sp_artistbrowse_add_ref(IntPtr artistBrowsePtr);

        [DllImport("libspotify")]
        public static extern sp_error sp_artistbrowse_release(IntPtr artistBrowsePtr);

    }

}
