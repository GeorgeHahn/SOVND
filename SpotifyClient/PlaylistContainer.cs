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
using libspotifydotnet.libspotify;

namespace SpotifyClient
{
    public class PlaylistContainer : IDisposable
    {
        private delegate void playlist_added_delegate(IntPtr containerPtr, IntPtr playlistPtr, int position, IntPtr userDataPtr);

        private delegate void playlist_removed_delegate(IntPtr containerPtr, IntPtr playlistPtr, int position, IntPtr userDataPtr);

        private delegate void playlist_moved_delegate(IntPtr containerPtr, IntPtr playlistPtr, int position, int new_position, IntPtr userDataPtr);

        private delegate void container_loaded_delegate(IntPtr containerPtr, IntPtr userDataPtr);

        private container_loaded_delegate fn_container_loaded_delegate;
        private playlist_added_delegate fn_playlist_added_delegate;
        private playlist_moved_delegate fn_playlist_moved_delegate;
        private playlist_removed_delegate fn_playlist_removed_delegate;

        private IntPtr _containerPtr;
        private IntPtr _callbacksPtr;
        private bool _disposed;

        private static PlaylistContainer _sessionContainer;

        public class PlaylistInfo
        {
            public IntPtr ContainerPtr;
            public IntPtr Pointer;
            public ulong FolderID;
            public sp_playlist_type PlaylistType;
            public string Name;
            public PlaylistInfo Parent;
            public List<PlaylistInfo> Children = new List<PlaylistInfo>();
        }

        private PlaylistContainer(IntPtr containerPtr)
        {
            if (containerPtr == IntPtr.Zero)
                throw new ArgumentNullException("containerPtr", "Container pointer is null");
            _containerPtr = containerPtr;
            initCallbacks();
        }

        #region IDisposable Members

        public void Dispose()
        {
            dispose(true);
            GC.SuppressFinalize(this);
        }

        ~PlaylistContainer()
        {
            dispose(false);
        }

        private void dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    safeRemoveCallbacks();
                }

                _disposed = true;
            }
        }

        #endregion IDisposable Members

        public static PlaylistContainer GetSessionContainer()
        {
            if (_sessionContainer == null)
            {
                if (Session.SessionPtr == IntPtr.Zero)
                    throw new InvalidOperationException("No valid session.");

                _sessionContainer = new PlaylistContainer(sp_session_playlistcontainer(Session.SessionPtr));
            }

            return _sessionContainer;
        }

        public static PlaylistContainer Get(IntPtr containerPtr)
        {
            return new PlaylistContainer(containerPtr);
        }

        private void initCallbacks()
        {
            this.fn_container_loaded_delegate = new container_loaded_delegate(this.container_loaded);
            this.fn_playlist_added_delegate = new playlist_added_delegate(this.playlist_added);
            this.fn_playlist_moved_delegate = new playlist_moved_delegate(this.playlist_moved);
            this.fn_playlist_removed_delegate = new playlist_removed_delegate(this.playlist_removed);

            sp_playlistcontainer_callbacks callbacks = new sp_playlistcontainer_callbacks();
            callbacks.container_loaded = Marshal.GetFunctionPointerForDelegate(this.fn_container_loaded_delegate);
            callbacks.playlist_added = Marshal.GetFunctionPointerForDelegate(this.fn_playlist_added_delegate);
            callbacks.playlist_moved = Marshal.GetFunctionPointerForDelegate(this.fn_playlist_moved_delegate);
            callbacks.playlist_removed = Marshal.GetFunctionPointerForDelegate(this.fn_playlist_removed_delegate);

            _callbacksPtr = Marshal.AllocHGlobal(Marshal.SizeOf(callbacks));
            Marshal.StructureToPtr(callbacks, _callbacksPtr, true);

            sp_playlistcontainer_add_callbacks(_containerPtr, _callbacksPtr, IntPtr.Zero);

            return;
        }

        public bool IsLoaded
        {
            get
            {
                return sp_playlistcontainer_is_loaded(_containerPtr);
            }
        }

        public bool PlaylistsAreLoaded
        {
            get
            {
                if (!this.IsLoaded)
                    return false;

                int count = sp_playlistcontainer_num_playlists(_containerPtr);

                for (int i = 0; i < count; i++)
                {
                    if (sp_playlistcontainer_playlist_type(_containerPtr, i) == sp_playlist_type.SP_PLAYLIST_TYPE_PLAYLIST)
                    {
                        using (Playlist p = new Playlist(sp_playlistcontainer_playlist(_containerPtr, i)))
                        {
                            if (!p.IsLoaded)
                                return false;
                        }
                    }
                }

                return true;
            }
        }

        public List<PlaylistInfo> GetAllPlaylists()
        {
            if (!GetSessionContainer().IsLoaded)
                throw new InvalidOperationException("Container is not loaded.");

            List<PlaylistInfo> playlists = new List<PlaylistInfo>();

            for (int i = 0; i < sp_playlistcontainer_num_playlists(_containerPtr); i++)
            {
                if (sp_playlistcontainer_playlist_type(_containerPtr, i) == sp_playlist_type.SP_PLAYLIST_TYPE_PLAYLIST)
                {
                    IntPtr playlistPtr = sp_playlistcontainer_playlist(_containerPtr, i);

                    playlists.Add(new PlaylistInfo()
                    {
                        Pointer = playlistPtr,
                        PlaylistType = sp_playlist_type.SP_PLAYLIST_TYPE_PLAYLIST,
                        ContainerPtr = _containerPtr,
                        Name = Functions.PtrToString(sp_playlist_name(playlistPtr))
                    });
                }
            }

            return playlists;
        }

        private void safeRemoveCallbacks()
        {
            try
            {
                if (_containerPtr == IntPtr.Zero)
                    return;

                if (_callbacksPtr == IntPtr.Zero)
                    return;

                sp_playlistcontainer_remove_callbacks(_containerPtr, _callbacksPtr, IntPtr.Zero);
            }
            catch { }
        }

        public PlaylistInfo FindContainer(ulong folderID)
        {
            if (!GetSessionContainer().IsLoaded)
                throw new InvalidOperationException("Session container is not loaded.");

            PlaylistInfo tree = buildTree();

            if (folderID == 0)
            {
                return tree;
            }
            else
            {
                return searchTreeRecursive(tree, folderID);
            }
        }

        public List<PlaylistInfo> GetChildren(ulong folderID)
        {
            return FindContainer(folderID).Children;
        }

        private PlaylistInfo searchTreeRecursive(PlaylistInfo tree, ulong folderID)
        {
            if (tree.FolderID == folderID)
                return tree;

            foreach (PlaylistInfo playlist in tree.Children)
            {
                if (playlist.PlaylistType == sp_playlist_type.SP_PLAYLIST_TYPE_START_FOLDER)
                {
                    if (playlist.FolderID == folderID)
                        return playlist;

                    PlaylistInfo p2 = searchTreeRecursive(playlist, folderID);

                    if (p2 != null)
                        return p2;
                }
            }

            return null;
        }

        private PlaylistInfo buildTree()
        {
            PlaylistInfo current = new PlaylistInfo();
            current.FolderID = ulong.MaxValue; //root

            for (int i = 0; i < sp_playlistcontainer_num_playlists(_containerPtr); i++)
            {
                PlaylistInfo playlist = new PlaylistInfo()
                {
                    PlaylistType = sp_playlistcontainer_playlist_type(_containerPtr, i),
                    ContainerPtr = _containerPtr
                };

                switch (playlist.PlaylistType)
                {
                    case sp_playlist_type.SP_PLAYLIST_TYPE_START_FOLDER:

                        playlist.FolderID = sp_playlistcontainer_playlist_folder_id(_containerPtr, i);
                        playlist.Name = GetFolderName(_containerPtr, i);
                        playlist.Parent = current;
                        current.Children.Add(playlist);
                        current = playlist;

                        break;

                    case sp_playlist_type.SP_PLAYLIST_TYPE_END_FOLDER:

                        current = current.Parent;
                        break;

                    case sp_playlist_type.SP_PLAYLIST_TYPE_PLAYLIST:

                        playlist.Pointer = sp_playlistcontainer_playlist(_containerPtr, i);
                        playlist.Parent = current;
                        current.Children.Add(playlist);

                        break;
                }
            }

            while (current.Parent != null)
            {
                current = current.Parent;
            }

            return current;
        }

        public string GetFolderName(IntPtr containerPtr, int index)
        {
            IntPtr namePtr = Marshal.AllocHGlobal(128);

            try
            {
                sp_error error = sp_playlistcontainer_playlist_folder_name(containerPtr, index, namePtr, 128);

                return Functions.PtrToString(namePtr);
            }
            finally
            {
                Marshal.FreeHGlobal(namePtr);
            }
        }

        private void container_loaded(IntPtr containerPtr, IntPtr userDataPtr)
        {
            Log.Debug(Plugin.LOG_MODULE, "container_loaded");
        }

        private void playlist_added(IntPtr containerPtr, IntPtr playlistPtr, int position, IntPtr userDataPtr)
        {
            Log.Debug(Plugin.LOG_MODULE, "playlist_added at position {0}", position);
        }

        private void playlist_moved(IntPtr containerPtr, IntPtr playlistPtr, int position, int new_position, IntPtr userDataPtr)
        {
            Log.Debug(Plugin.LOG_MODULE, "playlist_moved from {0} to {1}", position, new_position);
        }

        private void playlist_removed(IntPtr containerPtr, IntPtr playlistPtr, int position, IntPtr userDataPtr)
        {
            Log.Debug(Plugin.LOG_MODULE, "playlist_removed");
        }
    }
}