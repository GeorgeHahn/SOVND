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
    public class Artist : IDisposable
    {
        private bool _disposed;
        private IntPtr _browsePtr;
        private artistbrowse_complete_cb_delegate _d;

        public IntPtr ArtistPtr { get; private set; }

        public string Name { get; private set; }

        public bool IsBrowseComplete { get; private set; }

        public List<IntPtr> AlbumPtrs { get; private set; }

        public Artist(IntPtr artistPtr)
        {
            if (artistPtr == IntPtr.Zero)
                throw new InvalidOperationException("Artist pointer is null.");

            this.ArtistPtr = artistPtr;
            this.Name = Functions.PtrToString(libspotify.sp_artist_name(artistPtr));
        }

        #region IDisposable Members

        public void Dispose()
        {
            dispose(true);
            GC.SuppressFinalize(this);
        }

        ~Artist()
        {
            dispose(false);
        }

        private void dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    safeReleaseArtist();
                }

                _disposed = true;
            }
        }

        #endregion IDisposable Members

        private void safeReleaseArtist()
        {
            if (_browsePtr != IntPtr.Zero)
            {
                try
                {
                    // necessary metadata is destroyed if the browse is released here...
                    //libspotify.sp_artistbrowse_release(_browsePtr);
                }
                catch { }
            }
        }

        private void artistbrowse_complete(IntPtr result, IntPtr userDataPtr)
        {
            try
            {
                libspotify.sp_error error = libspotify.sp_artistbrowse_error(result);

                if (error != libspotify.sp_error.OK)
                {
                    Log.Error(Plugin.LOG_MODULE, "Artist browse failed: {0}", libspotify.sp_error_message(error));
                    return;
                }

                int numalbums = libspotify.sp_artistbrowse_num_albums(_browsePtr);

                List<IntPtr> albumPtrs = new List<IntPtr>();

                for (int i = 0; i < libspotify.sp_artistbrowse_num_albums(_browsePtr); i++)
                {
                    IntPtr albumPtr = libspotify.sp_artistbrowse_album(_browsePtr, i);

                    // excluding singles, compilations, and unknowns
                    if (libspotify.sp_album_type(albumPtr) == libspotify.sp_albumtype.SP_ALBUMTYPE_ALBUM
                        && libspotify.sp_album_is_available(albumPtr))
                        albumPtrs.Add(albumPtr);
                }

                this.AlbumPtrs = albumPtrs;

                this.IsBrowseComplete = true;
            }
            finally
            {
                safeReleaseArtist();
            }
        }

        public bool BeginBrowse()
        {
            try
            {
                _d = new artistbrowse_complete_cb_delegate(this.artistbrowse_complete);
                IntPtr callbackPtr = Marshal.GetFunctionPointerForDelegate(_d);
                _browsePtr = libspotify.sp_artistbrowse_create(Session.SessionPtr, this.ArtistPtr, libspotify.sp_artistbrowse_type.SP_ARTISTBROWSE_NO_TRACKS, callbackPtr, IntPtr.Zero);

                return true;
            }
            catch (Exception ex)
            {
                Log.Error(Plugin.LOG_MODULE, "Album.BeginBrowse() failed: {0}", ex.Message);
                return false;
            }
        }
    }
}