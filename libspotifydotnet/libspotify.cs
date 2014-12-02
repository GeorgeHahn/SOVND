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

        public enum sp_albumtype {
            SP_ALBUMTYPE_ALBUM = 0,
            SP_ALBUMTYPE_SINGLE = 1,
            SP_ALBUMTYPE_COMPILATION = 2,
            SP_ALBUMTYPE_UNKNOWN = 3
        }

        [DllImport("libspotify")]
        public static extern bool sp_album_is_loaded(IntPtr albumPtr);

        [DllImport("libspotify")]
        public static extern bool sp_album_is_available(IntPtr albumPtr);

        [DllImport("libspotify")]
        public static extern IntPtr sp_album_artist(IntPtr albumPtr);

        [DllImport("libspotify")]
        public static extern IntPtr sp_album_cover(IntPtr albumPtr, sp_image_size size);

        [DllImport("libspotify")]
        public static extern IntPtr sp_album_name(IntPtr albumPtr);

        [DllImport("libspotify")]
        public static extern int sp_album_year(IntPtr albumPtr);

        [DllImport("libspotify")]
        public static extern sp_albumtype sp_album_type(IntPtr albumPtr);

        [DllImport("libspotify")]
        public static extern sp_error sp_album_add_ref(IntPtr albumPtr);

        [DllImport("libspotify")]
        public static extern sp_error sp_album_release(IntPtr albumPtr);       

    }

}
