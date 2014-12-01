﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SpotifyClient;
using System.IO;
using System.Threading;
using System.Windows.Interop;
using SOVND.Lib;
using System.Diagnostics;
using SOVND.Lib.Settings;
using SOVND.Client.ViewModels;
using SOVND.Client.Views;
using System.ComponentModel;
using SOVND.Lib.Models;

namespace SOVND.Client
{
    public class AppName : IAppName
    {
        public string Name { get { return "SOVND_client"; } }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ISettingsProvider _settings;
        private readonly IAppName _appname;
        private SettingsModel auth;

        public MainWindow(ISettingsProvider settings, IAppName appname)
        {
            _settings = settings;
            _appname = appname;

            InitializeComponent();
           
            Loaded += (_, __) =>
            {
                SetupSettings();
                InitializeSpotify();
                SetupChannel();
            };

            Closed += (_, __) =>
            {
                App.client.Disconnect();
                Spotify.ShutDown();
                Process.GetCurrentProcess().Kill(); // TODO That's really inelegant
            };
        }

        private void SetupSettings()
        {
            if (!_settings.AuthSettingsSet())
            {
                SettingsWindow w = new SettingsWindow();
                var settingsViewModel = new SettingsViewModel(_settings.GetAuthSettings());
                w.DataContext = settingsViewModel;
                w.ShowDialog();
            }

            auth = _settings.GetAuthSettings();
        }

        private void InitializeSpotify()
        {
            App.client.WindowHandle = new WindowInteropHelper(this).Handle;
            App.uithread = SynchronizationContext.Current;
            SyncHolder.sync = SynchronizationContext.Current;
            Spotify.Initialize();
            if (!Spotify.Login(File.ReadAllBytes("spotify_appkey.key"), _appname.Name, auth.SpotifyUsername, auth.SpotifyPassword))
                throw new Exception("Login failure");

            while (!Spotify.Ready())
                Thread.Sleep(100);

            App.client.Run();
        }

        private void SetupChannel()
        {
            App.client.SubscribedChannelHandler.Subscribe();

            playlist = CollectionViewSource.GetDefaultView(App.client.SubscribedChannelHandler._playlist.Songs);

            // TODO this section needs to be scrapped //
            playlist.SortDescriptions.Clear();
            playlist.SortDescriptions.Add(new SortDescription("Votetime", ListSortDirection.Descending));
            playlist.SortDescriptions.Add(new SortDescription("Votes", ListSortDirection.Ascending));
            playlist.Refresh();
            ////////////////////////////////////////////

            Action Refresh = () => { SyncHolder.sync.Send((x) => playlist.Refresh(), null); };
            App.client.SubscribedChannelHandler.Songs.CollectionChanged += (_, __) => { Refresh(); };
            App.client.SubscribedChannelHandler._playlist.PropertyChanged += (_, __) => { Refresh(); };

            chatbox.ItemsSource = App.client.SubscribedChannelHandler.Chats;

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
            App.client.AddTrack(track);
        }

        private void SendChat(object sender, RoutedEventArgs e)
        {
            App.client.SendChat(chatinput.Text);
            chatinput.Clear();
        }

        private void NewChannel(object sender, RoutedEventArgs e)
        {
            var newch = new NewChannel();
            if (newch.ShowDialog().Value == true)
            {
                var channel = newch.ChannelName;
                App.client.SubscribeToChannel(channel);
            }
        }
    }
}
