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
        
        [DllImport("libspotify")]
        public static extern IntPtr sp_albumbrowse_create(IntPtr sessionPtr, IntPtr albumPtr, IntPtr callbackPtr, IntPtr userDataPtr);

        [DllImport("libspotify")]
        public static extern bool sp_albumbrowse_is_loaded(IntPtr albumBrowsePtr);

        [DllImport("libspotify")]
        public static extern sp_error sp_albumbrowse_error(IntPtr albumBrowsePtr);

        [DllImport("libspotify")]
        public static extern IntPtr sp_albumbrowse_album(IntPtr albumBrowsePtr);

        [DllImport("libspotify")]
        public static extern IntPtr sp_albumbrowse_artist(IntPtr albumBrowsePtr);

        [DllImport("libspotify")]
        public static extern int sp_albumbrowse_num_copyrights(IntPtr albumBrowsePtr);

        [DllImport("libspotify")]
        public static extern IntPtr sp_albumbrowse_copyright(IntPtr albumBrowsePtr, int index);

        [DllImport("libspotify")]
        public static extern int sp_albumbrowse_num_tracks(IntPtr albumBrowsePtr);

        [DllImport("libspotify")]
        public static extern IntPtr sp_albumbrowse_track(IntPtr albumBrowsePtr, int index);

        [DllImport("libspotify")]
        public static extern IntPtr sp_albumbrowse_review(IntPtr albumBrowsePtr);

        [DllImport("libspotify")]
        public static extern int sp_albumbrowse_backend_request_duration(IntPtr albumBrowsePtr);

        [DllImport("libspotify")]
        public static extern sp_error sp_albumbrowse_add_ref(IntPtr albumBrowsePtr);

        [DllImport("libspotify")]
        public static extern sp_error sp_albumbrowse_release(IntPtr albumBrowsePtr);

    }

}
