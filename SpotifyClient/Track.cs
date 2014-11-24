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

using libspotifydotnet;
using System.Diagnostics;

namespace SpotifyClient
{
    [DebuggerDisplay("{Name}")]
    public class Track
    {
        private List<string> _artists = new List<string>();

        public IntPtr TrackPtr { get; private set; }

        public string Name { get; private set; }

        public Album Album { get; private set; }

        public int TrackNumber { get; private set; }

        public decimal Seconds { get; private set; }

        public string SongID { get; private set; }

        public string[] Artists
        {
            get { return _artists.ToArray(); }
        }

        public string AllArtists
        {
            get { return string.Join(", ", Artists); }
        }

        public Track(string link)
        {
            SongID = link;
            IntPtr linkPtr = Functions.StringToLinkPtr(link);
            try
            {
                IntPtr trackPtr = libspotify.sp_link_as_track(linkPtr);
                init(trackPtr);
            }
            finally
            {
                if (linkPtr != IntPtr.Zero)
                    libspotify.sp_link_release(linkPtr);
            }
        }

        public Track(IntPtr trackPtr)
        {
            init(trackPtr);
        }

        private bool init(IntPtr trackPtr)
        {
            if (!libspotify.sp_track_is_loaded(trackPtr))
                return false;

            this.TrackPtr = trackPtr;
            this.Name = Functions.PtrToString(libspotify.sp_track_name(trackPtr));
            this.TrackNumber = libspotify.sp_track_index(trackPtr);
            this.Seconds = (decimal)libspotify.sp_track_duration(trackPtr) / 1000M;
            IntPtr albumPtr = libspotify.sp_track_album(trackPtr);
            if (albumPtr != IntPtr.Zero)
                this.Album = new Album(albumPtr);

            for (int i = 0; i < libspotify.sp_track_num_artists(trackPtr); i++)
            {
                IntPtr artistPtr = libspotify.sp_track_artist(trackPtr, i);
                if (artistPtr != IntPtr.Zero)
                    _artists.Add(Functions.PtrToString(libspotify.sp_artist_name(artistPtr)));
            }

            return true;
        }
    }
}