using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using SpotifyClient;
using System.IO;
using System.Threading;
using System.Windows.Interop;
using System.Diagnostics;
using SOVND.Lib.Settings;
using SOVND.Client.ViewModels;
using SOVND.Client.Views;
using System.ComponentModel;
using SOVND.Lib.Models;
using SOVND.Client.Modules;
using System.Collections;
using SOVND.Lib.Handlers;

namespace SOVND.Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ISettingsProvider _settings;
        private readonly IAppName _appname;
        private readonly SovndClient _client;
        private readonly NowPlayingHandler _player;
        private readonly SyncHolder _sync;
        private SettingsModel _auth;

        public MainWindow(ISettingsProvider settings, IAppName appname, SovndClient client, NowPlayingHandler player, SyncHolder sync)
        {
            InitializeComponent();

            _settings = settings;
            _appname = appname;
            _client = client;
            _player = player;
            _sync = sync;
            _auth = _settings.GetAuthSettings();

            Loaded += (_, __) =>
            {
                App.WindowHandle = new WindowInteropHelper(this).Handle;
                _sync.sync = SynchronizationContext.Current;

                _client.Run();
                _player.Run();

                SetupChannel();
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
            _client.SubscribedChannelHandler.Subscribe();
            _player.SubscribeTo("ambient");

            var observablePlaylist = ((IObservablePlaylistProvider)_client.SubscribedChannelHandler.Playlist);

            playlist = (ListCollectionView)(CollectionViewSource.GetDefaultView(observablePlaylist.Songs));
            playlist.CustomSort = new SongComparer();

            observablePlaylist.PropertyChanged += (_, __) =>
            {
                _sync.sync.Send((x) => playlist.Refresh(), null);
            };

            chatbox.ItemsSource = _client.SubscribedChannelHandler.Chats;

            BindToPlaylist();
        }

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
            _sync.sync.Send((x) => lbPlaylist.ItemsSource = playlist, null);
            _sync.sync.Send((x) => playlist.Refresh(), null);
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string text = tbSearch.Text;

            if (!string.IsNullOrWhiteSpace(text))
            {
                var shortlist = new List<Track>();
                var candidates = new List<Track>();

                if (searchToken != null)
                    searchToken.Cancel();

                searchToken = new CancellationTokenSource();
                var token = searchToken.Token;

                var searchTask = Task.Factory.StartNew(() =>
                {
                    var search = Spotify.GetSearch(text);

                    if (search != null)
                    {
                        foreach (var trackPtr in search?.TrackPtrs)
                        {
                            if (token.IsCancellationRequested)
                                return;

                            var trackLink = Spotify.GetTrackLink(trackPtr);
                            var track = new Track(trackLink);
                            candidates.Add(track);
                        }
                        _sync.sync.Send((x) => lbPlaylist.ItemsSource = candidates, null);
                    }
                }, token);
            }
            else
            {
                if (searchToken != null)
                    searchToken.Cancel();

                BindToPlaylist();
            }
        }

        private void AddSong_Click(object sender, RoutedEventArgs e)
        {
            var item = ((Button)sender).DataContext as Track;
            if (item == null)
                return;

            BindToPlaylist();

            EnqueueTrack(item);
            tbSearch.Clear();
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
    }
}
