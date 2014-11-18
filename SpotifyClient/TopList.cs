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

using libspotifydotnet;

namespace SpotifyClient
{
    public class TopList : IDisposable
    {
        private static readonly string LOG_MODULE = "Toplist";

        private bool _disposed;
        private IntPtr _browsePtr;
        private IntPtr _callbackPtr;
        private toplistbrowse_complete_cb_delegate _d;

        public delegate void toplistbrowse_complete_cb_delegate(IntPtr result, IntPtr userDataPtr);

        public bool IsLoaded { get; private set; }

        public List<IntPtr> Ptrs { get; private set; }

        public libspotify.sp_toplisttype ToplistType { get; private set; }

        #region IDisposable Members

        public void Dispose()
        {
            dispose(true);
            GC.SuppressFinalize(this);
        }

        ~TopList()
        {
            dispose(false);
        }

        private void dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    safeReleaseToplist();
                }

                _disposed = true;
            }
        }

        #endregion IDisposable Members

        public static TopList BeginBrowse(libspotify.sp_toplisttype type, int region)
        {
            try
            {
                TopList t = new TopList();
                t.ToplistType = type;
                t._d = new toplistbrowse_complete_cb_delegate(t.toplistbrowse_complete);
                t._callbackPtr = Marshal.GetFunctionPointerForDelegate(t._d);
                t._browsePtr = libspotify.sp_toplistbrowse_create(Session.SessionPtr, type, region, IntPtr.Zero, t._callbackPtr, IntPtr.Zero);
                return t;
            }
            catch (Exception ex)
            {
                Log.Warning(Plugin.LOG_MODULE, "TopList.BeginBrowse() failed: {0}", ex.Message);
                return null;
            }
        }

        public libspotify.sp_error GetBrowseError()
        {
            return libspotify.sp_toplistbrowse_error(_browsePtr);
        }

        private void toplistbrowse_complete(IntPtr result, IntPtr userDataPtr)
        {
            if (_browsePtr == IntPtr.Zero)
                throw new ApplicationException("Toplist browse is null");

            libspotify.sp_error error = libspotify.sp_toplistbrowse_error(_browsePtr);

            if (error != libspotify.sp_error.OK)
            {
                Log.Warning(Plugin.LOG_MODULE, "ERROR: Toplist browse failed: {0}", Functions.PtrToString(libspotify.sp_error_message(error)));
                this.IsLoaded = true;
                return;
            }

            if (_browsePtr == IntPtr.Zero)
                throw new ApplicationException("Toplist browse is null");

            int count = this.ToplistType == libspotify.sp_toplisttype.SP_TOPLIST_TYPE_ALBUMS ? libspotify.sp_toplistbrowse_num_albums(_browsePtr) : this.ToplistType == libspotify.sp_toplisttype.SP_TOPLIST_TYPE_ARTISTS ? libspotify.sp_toplistbrowse_num_artists(_browsePtr) : libspotify.sp_toplistbrowse_num_tracks(_browsePtr);

            List<IntPtr> ptrs = new List<IntPtr>();

            IntPtr tmp = IntPtr.Zero;

            for (int i = 0; i < count; i++)
            {
                if (this.ToplistType == libspotify.sp_toplisttype.SP_TOPLIST_TYPE_ALBUMS)
                {
                    tmp = libspotify.sp_toplistbrowse_album(_browsePtr, i);
                    if (libspotify.sp_album_is_available(tmp))
                        ptrs.Add(tmp);
                }
                else if (this.ToplistType == libspotify.sp_toplisttype.SP_TOPLIST_TYPE_ARTISTS)
                {
                    tmp = libspotify.sp_toplistbrowse_artist(_browsePtr, i);
                    ptrs.Add(tmp);
                }
                else
                {
                    tmp = libspotify.sp_toplistbrowse_track(_browsePtr, i);
                    ptrs.Add(tmp);
                }
            }
            this.Ptrs = ptrs;
            this.IsLoaded = true;
        }

        private void safeReleaseToplist()
        {
            if (_browsePtr != IntPtr.Zero)
            {
                try
                {
                    var err = libspotify.sp_toplistbrowse_release(_browsePtr);
                    if (err == libspotify.sp_error.OK)
                    {
                        Log.Trace(LOG_MODULE, "Toplist browse was released successfully");
                    }
                    else
                    {
                        Log.Warning(LOG_MODULE, "Toplist browse released with errors: {0}", err);
                    }
                }
                catch { }
                finally
                {
                    _browsePtr = IntPtr.Zero;
                }
            }
        }
    }
}