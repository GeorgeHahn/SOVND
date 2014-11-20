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

        public enum sp_toplisttype {
            SP_TOPLIST_TYPE_ARTISTS = 0,
            SP_TOPLIST_TYPE_ALBUMS = 1,
            SP_TOPLIST_TYPE_TRACKS = 2
        }

        public enum sp_toplistregion {
            SP_TOPLIST_REGION_EVERYWHERE = 0,
            SP_TOPLIST_REGION_USER = 1
        }

        [DllImport("libspotify")]
        public static extern IntPtr sp_toplistbrowse_create(IntPtr sessionPtr, sp_toplisttype type, int region, IntPtr usernamePtr, IntPtr browseCompleteCb, IntPtr userDataPtr);
        
        [DllImport("libspotify")]
        public static extern bool sp_toplistbrowse_is_loaded(IntPtr tlb);

        [DllImport("libspotify")]
        public static extern sp_error sp_toplistbrowse_error(IntPtr tlb);

        [DllImport("libspotify")]
        public static extern sp_error sp_toplistbrowse_add_ref(IntPtr tlb);

        [DllImport("libspotify")]
        public static extern sp_error sp_toplistbrowse_release(IntPtr tlb);

        [DllImport("libspotify")]
        public static extern int sp_toplistbrowse_num_artists(IntPtr tlbb);

        [DllImport("libspotify")]
        public static extern IntPtr sp_toplistbrowse_artist(IntPtr tlb, int index);

        [DllImport("libspotify")]
        public static extern int sp_toplistbrowse_num_albums(IntPtr tlb);

        [DllImport("libspotify")]
        public static extern IntPtr sp_toplistbrowse_album(IntPtr tlb, int index);

        [DllImport("libspotify")]
        public static extern int sp_toplistbrowse_num_tracks(IntPtr tlb);

        [DllImport("libspotify")]
        public static extern IntPtr sp_toplistbrowse_track(IntPtr tlb, int index);

        [DllImport("libspotify")]
        public static extern IntPtr sp_toplistbrowse_backend_request_duration(IntPtr tlb);

    }

}
