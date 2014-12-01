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

namespace SOVND.Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ISettingsProvider _settings;
        private readonly IAppName _appname;
        private SettingsModel _auth;

        public MainWindow(ISettingsProvider settings, IAppName appname)
        {
            _settings = settings;
            _appname = appname;
            
            InitializeComponent();

            _auth = _settings.GetAuthSettings();


            Loaded += (_, __) =>
            {
                App.WindowHandle = new WindowInteropHelper(this).Handle;
                App.UIThread = SynchronizationContext.Current;
                SyncHolder.sync = SynchronizationContext.Current;
                App.Client.Run();
                App.Player.Run();
                SetupChannel();
            };

            Closed += (_, __) =>
            {
                App.Client.Disconnect();
                Spotify.ShutDown();
                Process.GetCurrentProcess().Kill(); // TODO That's really inelegant
            };
        }

        private void SetupChannel()
        {
            App.Client.SubscribedChannelHandler.Subscribe();
            App.Player.SubscribeTo("ambient");

            playlist = CollectionViewSource.GetDefaultView(App.Client.SubscribedChannelHandler._playlist.Songs);

            // TODO this section needs to be scrapped //
            playlist.SortDescriptions.Clear();
            playlist.SortDescriptions.Add(new SortDescription("Votetime", ListSortDirection.Descending));
            playlist.SortDescriptions.Add(new SortDescription("Votes", ListSortDirection.Ascending));
            playlist.Refresh();
            ////////////////////////////////////////////

            Action Refresh = () => { SyncHolder.sync.Send((x) => playlist.Refresh(), null); };
            App.Client.SubscribedChannelHandler.Songs.CollectionChanged += (_, __) => { Refresh(); };
            App.Client.SubscribedChannelHandler._playlist.PropertyChanged += (_, __) => { Refresh(); };

            chatbox.ItemsSource = App.Client.SubscribedChannelHandler.Chats;

            BindToPlaylist();
        }

        private CancellationTokenSource searchToken = null;
        private ICollectionView playlist;

        private void BindToPlaylist()
        {
            SyncHolder.sync.Send((x) => lbPlaylist.ItemsSource = playlist, null);
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
                        SyncHolder.sync.Send((x) => lbPlaylist.ItemsSource = candidates, null);
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
            App.Client.AddTrack(track);
        }

        private void SendChat(object sender, RoutedEventArgs e)
        {
            App.Client.SendChat(chatinput.Text);
            chatinput.Clear();
        }

        private void NewChannel(object sender, RoutedEventArgs e)
        {
            var newch = new NewChannel();
            if (newch.ShowDialog().Value == true)
            {
                var channel = newch.ChannelName;
                App.Client.SubscribeToChannel(channel);
            }
        }
    }
}
