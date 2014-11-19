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
            if (!Spotify.Login(File.ReadAllBytes("spotify_appkey.key"), File.ReadAllText("username.key"), File.ReadAllText("password.key")))
                throw new Exception("Login failure");
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
            //lbPlaylist.ItemsSource = App.client.channels.FirstOrDefault().Value.SongsByID.Values; // Dirty hack
                lbPlaylist.ItemsSource = App.client.Playlist;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            EnqueueTrack((Track)((Button)sender).DataContext);
            tbSearch.Clear();
        }

        private void EnqueueTrack(Track track)
        {
            App.client.AddTrack(track);
        }
    }
}
