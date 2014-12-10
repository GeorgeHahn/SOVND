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
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Anotar.NLog;
using libspotifydotnet.libspotify;

namespace SpotifyClient
{
    // TODO This class should probably get the INPC treatment so WPF will update its view nicely as things load

    [DebuggerDisplay("{Name}")]
    public class Track : INotifyPropertyChanged
    {
        private List<string> _artists = new List<string>();

        public System.Drawing.Image _artwork;


        public IntPtr TrackPtr { get; private set; }

        public string Name { get; private set; }

        public Album Album { get; private set; }
        
        public System.Drawing.Image AlbumArt { get { return GetAlbumArtAsync().Result; } }

        public int TrackNumber { get; private set; }

        public decimal Seconds { get; private set; }

        public string SongID { get; private set; }

        public bool Loaded { get; private set; }

        public string[] Artists
        {
            get { return _artists.ToArray(); }
        }

        public string AllArtists
        {
            get { return string.Join(", ", Artists); }
        }

        public Action onLoad { get; set; }

        public Track(string link)
        {
            if (string.IsNullOrWhiteSpace(link))
                throw new ArgumentOutOfRangeException("link");
            
            IntPtr linkPtr = Functions.StringToLinkPtr(link);
            if (linkPtr != IntPtr.Zero)
            {
                SongID = link;

                try
                {
                    TrackPtr = sp_link_as_track(linkPtr);
                    sp_track_add_ref(TrackPtr);
                    Init();
                }
                finally
                {
                    if (linkPtr != IntPtr.Zero)
                        sp_link_release(linkPtr);
                }
            }

        }

        public Track(IntPtr trackPtr)
        {
            this.TrackPtr = trackPtr;
            SongID = Spotify.GetTrackLink(trackPtr);
            Init();
        }

        private Track()
        {
            
        }
        public static async Task<Track> CreateTrackAsync(IntPtr trackPtr)
        {
            var t = new Track {TrackPtr = trackPtr, SongID = Spotify.GetTrackLink(trackPtr)};
            await t.InitAsync();
            return t;
        }

        ~Track()
        {
            sp_track_release(TrackPtr);

            _artwork?.Dispose();
        }

        private static List<Track> ToInitialize = new List<Track>();
        private static readonly object toinit = new Object();
        private IntPtr _albumPtr;

        public static void Check()
        {
            if (ToInitialize.Count == 0)
                return;

            var remove = new List<Track>();

            lock (toinit)
            {
                foreach (var track in ToInitialize)
                {
                    track.Init();
                    if (track.Loaded)
                        remove.Add(track);
                }

                if(remove.Count == 0)
                    return;
                
                LogTo.Debug("Tracks loaded: {0}", remove.Count);
                foreach (var removal in remove)
                    ToInitialize.Remove(removal);
            }
        }

        public bool Init()
        {
            if (Loaded)
                return true;

            if (!sp_track_is_loaded(this.TrackPtr))
            {
                // Queue this track to get initted by spotify thread
                if (!ToInitialize.Contains(this))
                {
                    lock (toinit)
                    {
                        ToInitialize.Add(this);
                    }
                }
                return false;
            }

            this.Name = Functions.PtrToString(sp_track_name(this.TrackPtr));
            this.TrackNumber = sp_track_index(this.TrackPtr);
            this.Seconds = (decimal)sp_track_duration(this.TrackPtr) / 1000M;
            this._albumPtr = sp_track_album(this.TrackPtr);
            if (_albumPtr != IntPtr.Zero)
                this.Album = new Album(_albumPtr);

            for (int i = 0; i < sp_track_num_artists(this.TrackPtr); i++)
            {
                IntPtr artistPtr = sp_track_artist(this.TrackPtr, i);
                if (artistPtr != IntPtr.Zero)
                    _artists.Add(Functions.PtrToString(sp_artist_name(artistPtr)));
            }

            if (onLoad != null)
                onLoad();
            this.Loaded = true;
            RaisePropertyChanged("AlbumArt");
            return true;
        }

        public async Task<bool> InitAsync()
        {
            if (Loaded)
                return true;

            if (!sp_track_is_loaded(this.TrackPtr))
            {
                // Queue this track to get initted by spotify thread
                if (!ToInitialize.Contains(this))
                {
                    lock (toinit)
                    {
                        ToInitialize.Add(this);
                    }
                }
                return false;
            }

            this.Name = Functions.PtrToString(sp_track_name(this.TrackPtr));
            this.TrackNumber = sp_track_index(this.TrackPtr);
            this.Seconds = (decimal)sp_track_duration(this.TrackPtr) / 1000M;
            this._albumPtr = sp_track_album(this.TrackPtr);
            if (_albumPtr != IntPtr.Zero)
                this.Album = new Album(_albumPtr);

            for (int i = 0; i < sp_track_num_artists(this.TrackPtr); i++)
            {
                IntPtr artistPtr = sp_track_artist(this.TrackPtr, i);
                if (artistPtr != IntPtr.Zero)
                    _artists.Add(Functions.PtrToString(sp_artist_name(artistPtr)));
            }

            await GetAlbumArtAsync();

            if (onLoad != null)
                onLoad();
            this.Loaded = true;
            RaisePropertyChanged("AlbumArt");
            return true;
        }

        private async Task<System.Drawing.Image> GetAlbumArtAsync()
        {
            if (!Loaded)
                return null;

            if (_artwork != null)
                return _artwork;

            while (_artwork == null)
            {
                var buffer = GetAlbumArtBuffer();
                if (buffer != null)
                    _artwork = System.Drawing.Image.FromStream(new MemoryStream(buffer));
                if (_artwork == null)
                    await Task.Delay(25);
            }
            RaisePropertyChanged("AlbumArt");
            return _artwork;
        }

        private string GetAlbumArtLink()
        {
            return Spotify.GetAlbumArtLink(_albumPtr, sp_image_size.SP_IMAGE_SIZE_SMALL);
        }

        private byte[] GetAlbumArtBuffer()
        {
            return Spotify.GetAlbumArt(GetAlbumArtLink());
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}