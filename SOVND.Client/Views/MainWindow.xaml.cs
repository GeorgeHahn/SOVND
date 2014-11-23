using System;
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
        private readonly SettingsModel auth;

        public MainWindow(ISettingsProvider settings, IAppName appname)
        {
            _settings = settings;
            _appname = appname;
            InitializeComponent();
            
            App.uithread = SynchronizationContext.Current;
            SyncHolder.sync = SynchronizationContext.Current;

            if (!_settings.AuthSettingsSet())
            {
                SettingsWindow w = new SettingsWindow();
                var settingsViewModel = new SettingsViewModel(settings.GetAuthSettings());
                w.DataContext = settingsViewModel;
                w.ShowDialog();
                Process.GetCurrentProcess().Kill(); // TODO clean this up
            }

            auth = _settings.GetAuthSettings();

            Loaded += MainWindow_Loaded;
            Closed += (a, b) =>
            {
                App.client.Disconnect();
                Spotify.ShutDown();
                Process.GetCurrentProcess().Kill(); // TODO That's really inelegant
            };
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            App.client.WindowHandle = new WindowInteropHelper(this).Handle;

            Spotify.Initialize();
            if (!Spotify.Login(File.ReadAllBytes("spotify_appkey.key"), _appname.Name, auth.SpotifyUsername, auth.SpotifyPassword))
                throw new Exception("Login failure");

            while (!Spotify.Ready())
                Thread.Sleep(100);

            App.client.Run();
            App.client.SubscribedChannelHandler.Subscribe();
            BindToPlaylist();
        }

        private void BindToPlaylist()
        {
            lbPlaylist.ItemsSource = App.client.SubscribedChannelHandler._playlist.InOrder();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string text = tbSearch.Text;

            if (!string.IsNullOrWhiteSpace(text))
            {
                var candidates = new List<Track>();
                var search = Spotify.GetSearch(text);
                foreach (var trackPtr in search?.TrackPtrs)
                {
                    var trackLink = Spotify.GetTrackLink(trackPtr);
                    var track = new Track(trackLink);
                    candidates.Add(track);
                }

                lbPlaylist.ItemsSource = candidates;
            }
            else
            {
                BindToPlaylist();
            }
        }

        private void AddSong_Click(object sender, RoutedEventArgs e)
        {
            var item = ((Button)sender).DataContext as Track;
            if (item == null)
                return;

            EnqueueTrack(item);
            tbSearch.Clear();
        }

        private void VoteUp_Click(object sender, RoutedEventArgs e)
        {
            var item = ((Button)sender).DataContext as Song;
            if (item == null)
                return;

            EnqueueTrack(item.track);
            BindToPlaylist();
        }

        private void EnqueueTrack(Track track)
        {
            App.client.AddTrack(track);
        }

        private void SendChat(object sender, RoutedEventArgs e)
        {
            chatbox.ItemsSource = App.client.SubscribedChannelHandler.Chats;

            App.client.SendChat(chatinput.Text);
            chatinput.Clear();
        }
    }
}
