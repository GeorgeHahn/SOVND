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
using System.Threading;
using libspotifydotnet;

namespace SpotifyClient
{
    public static class Spotify
    {
        private delegate bool Test();

        public delegate void MainThreadMessageDelegate(object[] args);

        private static AutoResetEvent _programSignal;
        private static AutoResetEvent _mainSignal;
        private static Queue<MainThreadMessage> _mq = new Queue<MainThreadMessage>();
        private static bool _shutDown = false;
        private static object _syncObj = new object();
        private static object _initSync = new object();
        private static bool _initted = false;
        private static bool _isRunning = false;
        private static bool _isLoggedIn = false;
        private static Action<IntPtr> d_notify = new Action<IntPtr>(Session_OnNotifyMainThread);
        private static Action<IntPtr> d_on_logged_in = new Action<IntPtr>(Session_OnLoggedIn);
        private static Thread _t;
        private static object _loginLock = new object();

        private static readonly int REQUEST_TIMEOUT = 20;
        private static readonly string LOG_MODULE = "Spotify";

        private class MainThreadMessage
        {
            public MainThreadMessageDelegate d;
            public object[] payload;
        }

        static Spotify()
        {
            Session.OnNotifyMainThread += d_notify;
            Session.OnLoggedIn += d_on_logged_in;
        }

        public static bool IsRunning
        {
            get { return _isRunning; }
        }

        public static bool IsLoggedIn
        {
            get { return _isLoggedIn; }
        }

        public static bool Login(byte[] appkey, string username, string password)
        {
            lock (_initSync)
            {
                if (!_initted)
                    throw new ApplicationException("Spotify message thread not initialized");
            }

            lock (_loginLock)
            {
                if (_isLoggedIn)
                    return true;

                postMessage(Session.Login, new object[] { appkey, username, password });

                _programSignal.WaitOne();

                if (Session.LoginError != libspotify.sp_error.OK)
                {
                    Log.Error(Plugin.LOG_MODULE, "Login failed: {0}", libspotify.sp_error_message(Session.LoginError));
                    return false;
                }

                return true;
            }
        }

        public static void Initialize()
        {
            if (_initted)
                return;

            lock (_initSync)
            {
                try
                {
                    _programSignal = new AutoResetEvent(false);
                    _shutDown = false;
                    _t = new Thread(new ThreadStart(mainThread));
                    _t.Start();

                    _programSignal.WaitOne();

                    Log.Debug(Plugin.LOG_MODULE, "Message thread running...");

                    _initted = true;
                }
                catch
                {
                    Session.OnNotifyMainThread -= d_notify;
                    Session.OnLoggedIn -= d_on_logged_in;

                    if (_t != null)
                    {
                        try
                        {
                            _t.Abort();
                        }
                        catch { }
                        finally
                        {
                            _t = null;
                        }
                    }
                }
            }
        }

        public static int GetUserCountry()
        {
            return Session.GetUserCountry();
        }

        public static List<PlaylistContainer.PlaylistInfo> GetAllSessionPlaylists()
        {
            waitFor(delegate
            {
                return PlaylistContainer.GetSessionContainer().IsLoaded
                    && PlaylistContainer.GetSessionContainer().PlaylistsAreLoaded;
            }, REQUEST_TIMEOUT);

            return PlaylistContainer.GetSessionContainer().GetAllPlaylists();
        }

        public static List<PlaylistContainer.PlaylistInfo> GetPlaylists()
        {
            return GetPlaylists(0);
        }

        public static List<PlaylistContainer.PlaylistInfo> GetPlaylists(ulong folderID)
        {
            if (Session.SessionPtr == IntPtr.Zero)
                throw new ApplicationException("No session");

            waitFor(delegate
            {
                return PlaylistContainer.GetSessionContainer().IsLoaded
                    && PlaylistContainer.GetSessionContainer().PlaylistsAreLoaded;
            }, REQUEST_TIMEOUT);

            return PlaylistContainer.GetSessionContainer().GetChildren(folderID);
        }

        public static PlaylistContainer.PlaylistInfo GetPlaylistContainer(ulong folderID)
        {
            if (Session.SessionPtr == IntPtr.Zero)
                throw new ApplicationException("No session");

            waitFor(delegate
            {
                return PlaylistContainer.GetSessionContainer().IsLoaded
                    && PlaylistContainer.GetSessionContainer().PlaylistsAreLoaded;
            }, REQUEST_TIMEOUT);

            return PlaylistContainer.GetSessionContainer().FindContainer(folderID);
        }

        public static string GetAlbumLink(IntPtr albumPtr)
        {
            IntPtr linkPtr = libspotify.sp_link_create_from_album(albumPtr);
            return Functions.LinkPtrToString(linkPtr);
        }

        public static Album AlbumFromLink(string albumLink)
        {
            IntPtr linkPtr = Functions.StringToLinkPtr(albumLink);
            return new Album(libspotify.sp_link_as_album(linkPtr));
        }

        public static Playlist GetPlaylist(string playlistLink, bool needTracks)
        {
            if (Session.SessionPtr == IntPtr.Zero)
                throw new ApplicationException("No session");

            Playlist playlist = Playlist.FromLink(playlistLink);

            if (playlist == null)
                return null;

            bool success = waitFor(delegate
            {
                return playlist.IsLoaded && needTracks ? playlist.TracksAreLoaded : true;
            }, REQUEST_TIMEOUT);

            return playlist;
        }

        public static Playlist GetInboxPlaylist()
        {
            if (Session.SessionPtr == IntPtr.Zero)
                throw new ApplicationException("No session");

            IntPtr inboxPtr = IntPtr.Zero;

            try
            {
                inboxPtr = libspotify.sp_session_inbox_create(Session.SessionPtr);

                Playlist p = new Playlist(inboxPtr);

                bool success = waitFor(delegate
                {
                    return p.IsLoaded;
                }, REQUEST_TIMEOUT);

                return p;
            }
            finally
            {
                try
                {
                    if (inboxPtr != IntPtr.Zero)
                        libspotify.sp_playlist_release(inboxPtr);
                }
                catch { }
            }
        }

        public static Playlist GetStarredPlaylist()
        {
            if (Session.SessionPtr == IntPtr.Zero)
                throw new ApplicationException("No session");

            IntPtr starredPtr = IntPtr.Zero;

            try
            {
                starredPtr = libspotify.sp_session_starred_create(Session.SessionPtr);

                Playlist p = new Playlist(starredPtr);

                bool success = waitFor(delegate
                {
                    return p.IsLoaded;
                }, REQUEST_TIMEOUT);

                return p;
            }
            finally
            {
                try
                {
                    if (starredPtr != IntPtr.Zero)
                        libspotify.sp_playlist_release(starredPtr);
                }
                catch { }
            }
        }

        public static Search GetSearch(string keywords)
        {
            if (Session.SessionPtr == IntPtr.Zero)
                throw new ApplicationException("No session");

            var search = Search.BeginSearch(keywords);

            if (!waitFor(delegate
                {
                    return search.IsLoaded;
                }, REQUEST_TIMEOUT))
            {
                Log.Warning(LOG_MODULE, "Search timeout");
                return null;
            }

            var err = search.GetSearchError();
            if (err != libspotify.sp_error.OK)
            {
                Log.Warning(LOG_MODULE, "Search failed: {0}", err);
                return null;
            }
            return search;
        }

        public static TopList GetToplist(string data)
        {
            if (Session.SessionPtr == IntPtr.Zero)
                throw new ApplicationException("No session");

            string[] parts = data.Split("|".ToCharArray());

            int region = parts[0].Equals("ForMe") ? (int)libspotify.sp_toplistregion.SP_TOPLIST_REGION_USER : parts[0].Equals("Everywhere") ? (int)libspotify.sp_toplistregion.SP_TOPLIST_REGION_EVERYWHERE : Convert.ToInt32(parts[0]);
            libspotify.sp_toplisttype type = parts[1].Equals("Artists") ? libspotify.sp_toplisttype.SP_TOPLIST_TYPE_ARTISTS : parts[1].Equals("Albums") ? libspotify.sp_toplisttype.SP_TOPLIST_TYPE_ALBUMS : libspotify.sp_toplisttype.SP_TOPLIST_TYPE_TRACKS;

            TopList toplist = TopList.BeginBrowse(type, region);

            bool success = waitFor(delegate
            {
                return toplist.IsLoaded;
            }, REQUEST_TIMEOUT);

            var err = toplist.GetBrowseError();
            if (err != libspotify.sp_error.OK)
            {
                Log.Warning(LOG_MODULE, "Toplist browse failed: {0}", err);
                return null;
            }

            return toplist;
        }

        public static string GetTrackLink(IntPtr trackPtr)
        {
            return GetTrackLink(trackPtr, 0);
        }

        public static string GetTrackLink(IntPtr trackPtr, int offset)
        {
            if (Session.SessionPtr == IntPtr.Zero)
                throw new ApplicationException("No session");

            IntPtr linkPtr = libspotify.sp_link_create_from_track(trackPtr, offset);
            try
            {
                return Functions.LinkPtrToString(linkPtr);
            }
            finally
            {
                if (linkPtr != IntPtr.Zero)
                    libspotify.sp_link_release(linkPtr);
            }
        }

        public static byte[] GetAlbumArt(string link)
        {
            if (Session.SessionPtr == IntPtr.Zero)
                throw new ApplicationException("No session");

            IntPtr linkPtr = Functions.StringToLinkPtr(link);
            if (linkPtr == IntPtr.Zero)
                return null;
            try
            {
                IntPtr coverPtr = libspotify.sp_image_create_from_link(Session.SessionPtr, linkPtr);
                using (Image img = Image.Load(coverPtr))
                {
                    waitFor(delegate()
                    {
                        return img.IsLoaded;
                    }, REQUEST_TIMEOUT);

                    var err = img.GetLoadError();
                    if (err != libspotify.sp_error.OK)
                    {
                        throw new ApplicationException(String.Format("Image failed to load: {0}", err));
                    }

                    int bytes = 0;
                    IntPtr bufferPtr = libspotify.sp_image_data(img.ImagePtr, out bytes);
                    byte[] buffer = new byte[bytes];
                    Marshal.Copy(bufferPtr, buffer, 0, buffer.Length);

                    return buffer;
                }
            }
            finally
            {
                if (linkPtr != IntPtr.Zero)
                    libspotify.sp_link_release(linkPtr);
            }
        }

        public static IntPtr[] GetAlbumTracks(string albumLink)
        {
            if (Session.SessionPtr == IntPtr.Zero)
                throw new ApplicationException("No session");

            using (Album album = Spotify.AlbumFromLink(albumLink))
            {
                if (!waitFor(delegate
                {
                    return libspotify.sp_album_is_loaded(album.AlbumPtr);
                }, REQUEST_TIMEOUT))
                    Log.Debug(Plugin.LOG_MODULE, "GetAlbumTracks() TIMEOUT waiting for album to load");

                if (album.BeginBrowse())
                {
                    if (!waitFor(delegate()
                    {
                        return album.IsBrowseComplete;
                    }, REQUEST_TIMEOUT))
                        Log.Debug(Plugin.LOG_MODULE, "GetAlbumTracks() TIMEOUT waiting for browse to complete");
                }

                if (album.TrackPtrs == null)
                    return null;

                return album.TrackPtrs.ToArray();
            }
        }

        public static string GetPlaylistLink(IntPtr playlistPtr)
        {
            IntPtr linkPtr = libspotify.sp_link_create_from_playlist(playlistPtr);
            return Functions.LinkPtrToString(linkPtr);
        }

        public static string GetArtistLink(IntPtr artistPtr)
        {
            IntPtr linkPtr = libspotify.sp_link_create_from_artist(artistPtr);
            return Functions.LinkPtrToString(linkPtr);
        }

        public static Artist ArtistFromLink(string artistLink)
        {
            IntPtr linkPtr = Functions.StringToLinkPtr(artistLink);
            return new Artist(libspotify.sp_link_as_artist(linkPtr));
        }

        public static IntPtr[] GetArtistAlbums(string artistLink)
        {
            if (Session.SessionPtr == IntPtr.Zero)
                throw new ApplicationException("No session");

            using (Artist artist = ArtistFromLink(artistLink))
            {
                if (!waitFor(delegate
                {
                    return libspotify.sp_artist_is_loaded(artist.ArtistPtr);
                }, REQUEST_TIMEOUT))
                    Log.Debug(Plugin.LOG_MODULE, "GetArtistAlbums() TIMEOUT waiting for artist to load");

                if (artist.BeginBrowse())
                {
                    if (!waitFor(delegate()
                    {
                        return artist.IsBrowseComplete;
                    }, REQUEST_TIMEOUT))
                        Log.Debug(Plugin.LOG_MODULE, "GetArtistAlbums() TIMEOUT waiting for browse to complete");
                }

                if (artist.AlbumPtrs == null)
                    return null;

                return artist.AlbumPtrs.ToArray();
            }
        }

        public static PlaylistContainer GetUserPlaylists(IntPtr userPtr)
        {
            if (Session.SessionPtr == IntPtr.Zero)
                throw new ApplicationException("No session");

            IntPtr ptr = IntPtr.Zero;

            try
            {
                ptr = libspotify.sp_session_publishedcontainer_for_user_create(Session.SessionPtr, GetUserCanonicalNamePtr(userPtr));

                PlaylistContainer c = PlaylistContainer.Get(ptr);

                waitFor(delegate
                {
                    return c.IsLoaded
                        && c.PlaylistsAreLoaded;
                }, REQUEST_TIMEOUT);

                return c;
            }
            finally
            {
                //try {
                //    if (ptr != IntPtr.Zero)
                //        libspotify.sp_playlistcontainer_release(ptr);

                //} catch { }
            }
        }

        public static IntPtr GetUserCanonicalNamePtr(IntPtr userPtr)
        {
            if (Session.SessionPtr == IntPtr.Zero)
                throw new ApplicationException("No session");

            waitFor(delegate()
            {
                return libspotify.sp_user_is_loaded(userPtr);
            }, REQUEST_TIMEOUT);

            return libspotify.sp_user_canonical_name(userPtr);
        }

        private static bool waitFor(Test t, int timeout)
        {
            DateTime start = DateTime.Now;

            while (DateTime.Now.Subtract(start).Seconds < timeout)
            {
                if (t.Invoke())
                {
                    return true;
                }

                Thread.Sleep(10);
            }

            return false;
        }

        public static void ShutDown()
        {
            lock (_syncObj)
            {
                if (_mainSignal != null)
                    _mainSignal.Set();
                _mainSignal = null;
                _t = null;
                _shutDown = true;

                if (Session.SessionPtr != IntPtr.Zero)
                {
                    try
                    {
                        //libspotify.sp_error err = libspotify.sp_session_player_unload(Session.SessionPtr);
                        //err = libspotify.sp_session_logout(Session.SessionPtr);
                        //err = libspotify.sp_session_release(Session.SessionPtr);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(LOG_MODULE, "Error cleaning up session", ex);
                    }
                }
                _isLoggedIn = false;
                _initted = false;
            }

            _programSignal.WaitOne(2000, false);
            _programSignal = null;
        }

        private static void mainThread()
        {
            try
            {
                _mainSignal = new AutoResetEvent(false);

                int timeout = Timeout.Infinite;
                DateTime lastEvents = DateTime.MinValue;

                _isRunning = true;
                _programSignal.Set(); // this signals to program thread that loop is running

                while (true)
                {
                    if (_shutDown)
                        break;

                    _mainSignal.WaitOne(timeout, false);

                    if (_shutDown)
                        break;

                    lock (_syncObj)
                    {
                        try
                        {
                            if (Session.SessionPtr != IntPtr.Zero)
                            {
                                do
                                {
                                    libspotify.sp_session_process_events(Session.SessionPtr, out timeout);
                                } while (!_shutDown && timeout == 0);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Debug(Plugin.LOG_MODULE, "Exception invoking sp_session_process_events", ex);
                        }

                        while (_mq.Count > 0)
                        {
                            MainThreadMessage m = _mq.Dequeue();
                            m.d.Invoke(m.payload);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(Plugin.LOG_MODULE, "mainThread() unhandled exception", ex);
            }
            finally
            {
                _isRunning = false;
                if (_programSignal != null)
                    _programSignal.Set();
            }
        }

        public static void Session_OnLoggedIn(IntPtr obj)
        {
            if (Session.LoginError == libspotify.sp_error.OK)
                _isLoggedIn = true;
            if (_programSignal != null)
                _programSignal.Set();
        }

        public static void Session_OnNotifyMainThread(IntPtr sessionPtr)
        {
            if (_mainSignal != null)
                _mainSignal.Set();
        }

        private static void postMessage(MainThreadMessageDelegate d, object[] payload)
        {
            if (_mq == null)
                throw new ApplicationException("Message queue has not been initialized");

            _mq.Enqueue(new MainThreadMessage() { d = d, payload = payload });

            lock (_syncObj)
            {
                _mainSignal.Set();
            }
        }
    }
}