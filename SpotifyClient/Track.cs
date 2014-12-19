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
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Anotar.NLog;
using JetBrains.Annotations;
using libspotifydotnet;

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

        public System.Drawing.Image AlbumArt
        {
            get { return _artwork; }
        }

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

        private bool fetchArt;

        public Track(string link)
            : this(link, true)
        { }

        public Track(string link, bool fetchAlbumArt)
        {
            if (string.IsNullOrWhiteSpace(link))
                throw new ArgumentOutOfRangeException("link");
            
            IntPtr linkPtr = Functions.StringToLinkPtr(link);
            if (linkPtr != IntPtr.Zero)
            {
                SongID = link;
                fetchArt = fetchAlbumArt;
                try
                {
                    TrackPtr = libspotify.sp_link_as_track(linkPtr);
                    libspotify.sp_track_add_ref(TrackPtr);
                    InitAsync();
                }
                finally
                {
                    if (linkPtr != IntPtr.Zero)
                        libspotify.sp_link_release(linkPtr);
                }
            }

        }

        public Track(IntPtr trackPtr)
            :this(trackPtr, true)
        { }

        public Track(IntPtr trackPtr, bool fetchAlbumArt)
        {
            this.TrackPtr = trackPtr;
            fetchArt = fetchAlbumArt;
            SongID = Spotify.GetTrackLink(trackPtr);
            libspotify.sp_track_add_ref(TrackPtr);
            InitAsync();
        }

        public static async Task<Track> CreateTrackAsync(IntPtr trackPtr)
        {
            var t = new Track(trackPtr);
            t.SongID = Spotify.GetTrackLink(trackPtr);
            await t.InitAsync();
            return t;
        }

        ~Track()
        {
            libspotify.sp_track_release(TrackPtr);
            if(_artwork != null)
                _artwork.Dispose();
        }

        private static List<Track> ToInitialize = new List<Track>();
        private IntPtr _albumPtr;

        public async static Task Check()
        {
            if (ToInitialize.Count == 0)
                return;

            var remove = new List<Track>();

            foreach (var track in ToInitialize)
            {
                await track.InitAsync();
                if (track.Loaded)
                    remove.Add(track);
            }

            if(remove.Count == 0)
                return;
                
            foreach (var removal in remove)
                ToInitialize.Remove(removal);
        }

        public async Task<bool> InitAsync()
        {
            if (Loaded)
                return true;

            if (!libspotify.sp_track_is_loaded(this.TrackPtr))
            {
                // Queue this track to get initted by spotify thread
                if (!ToInitialize.Contains(this))
                    ToInitialize.Add(this);
                return false;
            }

            this.Name = Functions.PtrToString(libspotify.sp_track_name(this.TrackPtr));
            this.TrackNumber = libspotify.sp_track_index(this.TrackPtr);
            this.Seconds = (decimal) libspotify.sp_track_duration(this.TrackPtr) / 1000M;
            this._albumPtr = libspotify.sp_track_album(this.TrackPtr);
            if (_albumPtr != IntPtr.Zero)
                this.Album = new Album(_albumPtr);

            for (int i = 0; i < libspotify.sp_track_num_artists(this.TrackPtr); i++)
            {
                IntPtr artistPtr = libspotify.sp_track_artist(this.TrackPtr, i);
                if (artistPtr != IntPtr.Zero)
                    _artists.Add(Functions.PtrToString(libspotify.sp_artist_name(artistPtr)));
            }
            
            if(fetchArt)
                await GetAlbumArtAsync();

            if (onLoad != null)
                onLoad();
            this.Loaded = true;
            return true;
        }

        private async Task<System.Drawing.Image> GetAlbumArtAsync()
        {
            if (_artwork != null)
                return _artwork;

            try
            {
                int timeout = 1;
                int tries = 0;
                while (_artwork == null)
                {
                    libspotify.sp_image_size size = libspotify.sp_image_size.SP_IMAGE_SIZE_SMALL;
                    if (timeout < 30)
                        timeout = timeout*2;
                    else
                    {
                        tries++;
                        if(tries % 3 == 0)
                            size = libspotify.sp_image_size.SP_IMAGE_SIZE_SMALL;
                        else if (tries % 3 == 1)
                            size = libspotify.sp_image_size.SP_IMAGE_SIZE_NORMAL;
                        else if (tries % 3 == 2)
                            size = libspotify.sp_image_size.SP_IMAGE_SIZE_LARGE;
                    }
                    var buffer = GetAlbumArtBuffer(timeout, size);
                    if (buffer != null)
                        _artwork = System.Drawing.Image.FromStream(new MemoryStream(buffer));
                    if (_artwork == null)
                        await Task.Delay(250);
                }
                RaisePropertyChanged("AlbumArt");
                return _artwork;
            }
            catch (Exception e)
            {
                LogTo.ErrorException("Album art error", e);
                return null;
            }
        }

        private string GetAlbumArtLink(libspotify.sp_image_size artSize = libspotify.sp_image_size.SP_IMAGE_SIZE_SMALL)
        {
            var image = Spotify.GetAlbumArtLink(_albumPtr, artSize);
            if (image == null)
                image = Spotify.GetAlbumArtLink(_albumPtr, libspotify.sp_image_size.SP_IMAGE_SIZE_SMALL);
            if (image == null)
                image = Spotify.GetAlbumArtLink(_albumPtr, libspotify.sp_image_size.SP_IMAGE_SIZE_NORMAL);
            if (image == null)
                image = Spotify.GetAlbumArtLink(_albumPtr, libspotify.sp_image_size.SP_IMAGE_SIZE_LARGE);
            if(image == null)
                throw new Exception("Couldn't get art link");
            return image;
        }

        private string artlink;
        private byte[] GetAlbumArtBuffer(int timeout = 2, libspotify.sp_image_size size = libspotify.sp_image_size.SP_IMAGE_SIZE_SMALL)
        {
            if(string.IsNullOrEmpty(artlink))
                artlink = GetAlbumArtLink(size);
            return Spotify.GetAlbumArt(artlink, timeout);
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