using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using BugSense;
using BugSense.Core.Model;
using SOVND.Client.Modules;
using SOVND.Client.Util;
using SOVND.Client.ViewModels;
using SOVND.Client.Views;
using SOVND.Lib.Handlers;
using SOVND.Lib.Models;
using SOVND.Lib.Settings;
using SpotifyClient;

namespace SOVND.Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly SovndClient _client;
        private readonly IPlayerFactory _playerFactory;
        private NowPlayingHandler _player;
        private SynchronizationContext synchronization;

        public MainWindow(SovndClient client, ChannelDirectory channels, IPlayerFactory playerFactory)
        {
            InitializeComponent();

            _client = client;
            _playerFactory = playerFactory;

            channelbox.ItemsSource = channels.channels;
            
            Loaded += (_, __) =>
            {
                BindingOperations.EnableCollectionSynchronization(channels.channels, channels.channels);
                App.WindowHandle = new WindowInteropHelper(this).Handle;
                synchronization = SynchronizationContext.Current;

                _client.Run();

                // if(preferences_for_channel_set)
                //      SetupChannel(channel_pref);
            };

            Closed += (_, __) =>
            {
                _client.Disconnect();
                Spotify.ShutDown();
                Process.GetCurrentProcess().Kill(); // TODO That's really inelegant
            };
        }

        private void SetupChannel()
        {
            var observablePlaylist = ((IObservablePlaylistProvider)_client.SubscribedChannelHandler.Playlist);

            playlist = (ListCollectionView)(CollectionViewSource.GetDefaultView(observablePlaylist.Songs));
            playlist.CustomSort = new SongComparer();

            // TODO: Is this bad? Seems like it's a recipe for leaking - probably holds a ref to playlist.Songs and handler.Chats
            synchronization.Send((_) =>
            {
                BindingOperations.EnableCollectionSynchronization(observablePlaylist.Songs, observablePlaylist.Songs);
                BindingOperations.EnableCollectionSynchronization(_client.SubscribedChannelHandler.Chats, _client.SubscribedChannelHandler.Chats);
            }, null);

            chatbox.ItemsSource = _client.SubscribedChannelHandler.Chats;

            observablePlaylist.PropertyChanged += OnObservablePlaylistOnPropertyChanged;
            BindToPlaylist();
        }

        private void DropChannel()
        {
            _player?.Disconnect();
            lbPlaylist.ItemsSource = null;

            if (_client?.SubscribedChannelHandler?.Playlist != null)
            {
                var observablePlaylist = ((IObservablePlaylistProvider)_client.SubscribedChannelHandler.Playlist);
                observablePlaylist.PropertyChanged -= OnObservablePlaylistOnPropertyChanged;
            }

            _client?.SubscribedChannelHandler?.ShutdownHandler();

            playlist = null;
            chatbox.ItemsSource = null;
        }

        private void OnObservablePlaylistOnPropertyChanged(object _, PropertyChangedEventArgs __)
        {
            synchronization.Send(x => playlist.Refresh(), null);
        }

        private void Refresh() => OnObservablePlaylistOnPropertyChanged(null, null);


        class SongComparer : IComparer
        {
            public int Compare(object x, object y)
            {
                var xs = x as Song;
                var ys = y as Song;
                if (xs == null || ys == null)
                    throw new ArgumentOutOfRangeException("Both objects must be of type Song");

                return xs.CompareTo(y);
            }
        }

        private CancellationTokenSource searchToken = null;
        private ListCollectionView playlist;

        private void BindToPlaylist()
        {
            if (playlist == null)
                return;

            lbPlaylist.ItemsSource = playlist;
            Refresh();
        }

        private object searchlock = new object();
        private async void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string text = tbSearch.Text;

            if (!string.IsNullOrWhiteSpace(text))
            {
                var shortlist = new List<Track>();
                var candidates = new List<Track>();

                if (searchToken != null)
                {
                    searchToken.Cancel();
                    // TODO accessviolation from libspotify if we continue and a track's albumart happens to be in the process of fetching
                }

                searchToken = new CancellationTokenSource();
                var token = searchToken.Token;

                lock (searchlock)
                {
                    var search = Spotify.GetSearch(text);

                    if (search != null)
                    {
                        try
                        {
                            Task.Run(() =>
                            {
                                foreach (var trackPtr in search?.TrackPtrs)
                                {
                                    if (token.IsCancellationRequested)
                                        return;

                                    var track = new Track(trackPtr);
                                    candidates.Add(track);
                                }
                                synchronization.Send((_) => lbPlaylist.ItemsSource = candidates, null);
                            }, token);
                        }
                        catch (TaskCanceledException)
                        {
                        }
                    }
                }
            }
            else
            {
                if (searchToken != null)
                    searchToken.Cancel();
                Track.Check();
                BindToPlaylist();
            }
        }

        private async void AddSong_Click(object sender, RoutedEventArgs e)
        {
            var item = ((Button)sender).DataContext as Track;
            if (item == null)
                return;

            BindToPlaylist();

            EnqueueTrack(item);
            tbSearch.Clear();

            await BugSenseHandler.Instance.SendEventAsync("Added song");
        }
        
        private void VoteUp_Click(object sender, RoutedEventArgs e)
        {
            var item = ((Button)sender).DataContext as Song;
            if (item == null)
                return;

            EnqueueTrack(item.track);
        }

        private void EnqueueTrack(Track track)
        {
            _client.AddTrack(track);
        }

        private void SendChat(object sender, RoutedEventArgs e)
        {
            _client.SendChat(chatinput.Text);
            chatinput.Clear();
        }

        private void NewChannel(object sender, RoutedEventArgs e)
        {
            var newch = new NewChannel(new NewChannelViewModel(_client));
            if (newch.ShowDialog().Value == true)
            {
                var channel = newch.ChannelName;
                _client.SubscribeToChannel(channel);
            }
        }

        private void Del_Click(object sender, RoutedEventArgs e)
        {
            var item = ((Button)sender).DataContext as Song;
            if (item == null)
                return;

            _client.DeleteSong(item);
        }

        private void Blk_Click(object sender, RoutedEventArgs e)
        {
            var item = ((Button)sender).DataContext as Song;
            if (item == null)
                return;

            _client.BlockSong(item);
        }

        private void Rpt_Click(object sender, RoutedEventArgs e)
        {
            var item = ((Button)sender).DataContext as Song;
            if (item == null)
                return;

            _client.ReportSong(item);
        }

        private void Channelbox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            var channel = channelbox.SelectedItem as Channel;
            if (channel != null)
            {
                DropChannel();

                _player = _playerFactory.CreatePlayer(channel.Name);
                _client.SubscribeToChannel(channel.Name);
                SetupChannel();
            }
        }

        private void Chatinput_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                SendChat(null, null);
            }
        }
    }
}
