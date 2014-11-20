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

        public enum sp_relation_type {
            SP_RELATION_TYPE_UNKNOWN = 0,
            SP_RELATION_TYPE_NONE = 1,
            SP_RELATION_TYPE_UNIDIRECTIONAL = 2,
            SP_RELATION_TYPE_BIDIRECTIONAL = 3
        }

        [DllImport("libspotify")]
        public static extern IntPtr sp_user_canonical_name(IntPtr userPtr);

        [DllImport("libspotify")]
        public static extern IntPtr sp_user_display_name(IntPtr userPtr);

        [DllImport("libspotify")]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool sp_user_is_loaded(IntPtr userPtr);        

        [DllImport("libspotify")]
        public static extern sp_error sp_user_add_ref(IntPtr userPtr);

        [DllImport("libspotify")]
        public static extern sp_error sp_user_release(IntPtr userPtr);

    }

}
