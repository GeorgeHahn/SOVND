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

namespace SOVND.Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Spotify.Initialize();
            if (!Spotify.Login(File.ReadAllBytes("spotify_appkey.key"), "SOVND_client", File.ReadAllText("username.key"), File.ReadAllText("password.key")))
                throw new Exception("Login failure");

            while (!Spotify.Ready())
                Thread.Sleep(100);

            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            App.client.Run();
            App.client.SubscribedChannel.Subscribe();
            App.client.RegisterChannel("ambient", "Ambient music :)", "");
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string text = tbSearch.Text;

            if (!string.IsNullOrWhiteSpace(text))
            {
                var candidates = new List<Track>();
                var search = Spotify.GetSearch(text);
                foreach (var trackPtr in search.TrackPtrs)
                {
                    var trackLink = Spotify.GetTrackLink(trackPtr);
                    var track = new Track(trackLink);
                    candidates.Add(track);
                }

                lbPlaylist.ItemsSource = candidates;
            }
            else
            {
                lbPlaylist.ItemsSource = App.client.SubscribedChannel.Playlist.InOrder();
            }
        }

        private void AddSong_Click(object sender, RoutedEventArgs e)
        {
            var item = ((Button) sender).DataContext as Track;
            if (item == null)
                return;

            EnqueueTrack(item);
            tbSearch.Clear();
        }

        private void EnqueueTrack(Track track)
        {
            App.client.AddTrack(track);
        }
    }
}
