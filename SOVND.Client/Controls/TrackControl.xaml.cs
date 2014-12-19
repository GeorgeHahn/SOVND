using System;
using System.Windows;
using SpotifyClient;

namespace SOVND.Client.Controls
{
    public partial class TrackControl
    {
        public static readonly DependencyProperty TrackProperty = DependencyProperty.Register("Track", typeof (Track), typeof (TrackControl), new PropertyMetadata(default(Track)));

        public TrackControl()
        {
            InitializeComponent();
        }

        public Track Track
        {
            get { return (Track) GetValue(TrackProperty); }
            set { SetValue(TrackProperty, value); }
        }

        public event EventHandler OnSongAdd;

        private void AddSong_Click(object sender, RoutedEventArgs e)
        {
            if (OnSongAdd != null)
                OnSongAdd(sender, e);
        }
    }
}