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

        public enum sp_error {
            OK = 0,
            BAD_API_VERSION = 1,
            API_INITIALIZATION_FAILED = 2,
            TRACK_NOT_PLAYABLE = 3,            
            APPLICATION_KEY = 5,
            BAD_USERNAME_OR_PASSWORD = 6,
            USER_BANNED = 7,
            UNABLE_TO_CONTACT_SERVER = 8,
            CLIENT_TOO_OLD = 9,
            OTHER_PERMANENT = 10,
            BAD_USER_AGENT = 11,
            MISSING_CALLBACK = 12,
            INVALID_INDATA = 13,
            INDEX_OUT_OF_RANGE = 14,
            USER_NEEDS_PREMIUM = 15,
            OTHER_TRANSIENT = 16,
            IS_LOADING = 17,
            NO_STREAM_AVAILABLE = 18,
            PERMISSION_DENIED = 19,
            INBOX_IS_FULL = 20,
            NO_CACHE = 21,
            NO_SUCH_USER = 22,
            NO_CREDENTIALS = 23,
            NETWORK_DISABLED = 24,
            INVALID_DEVICE_ID = 25,
            CANT_OPEN_TRACE_FILE = 26,
            APPLICATION_BANNED = 27,
            OFFLINE_TOO_MANY_TRACKS = 31,
            OFFLINE_DISK_CACHE = 32,
            OFFLINE_EXPIRED = 33,
            OFFLINE_NOT_ALLOWED = 34,
            OFFLINE_LICENSE_LOST = 35,
            OFFLINE_LICENSE_ERROR = 36 
        }

        [DllImport("libspotify")]
        public static extern IntPtr sp_error_message(sp_error error);
        
    }

}
