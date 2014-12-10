using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Anotar.NLog;
using SOVND.Lib.Models;

namespace SOVND.Lib.Handlers
{
    public class ObservablePlaylistProvider : PlaylistProviderBase, IObservablePlaylistProvider
    {
        private readonly ObservableCollection<Song> _songs;

        public ObservableCollection<Song> Songs
        {
            get { return _songs; }
        }

        internal override void AddSong(Song song)
        {
            _songs.Add(song);
            song.PropertyChanged += Song_PropertyChanged;

            RaisePropertyChanged("Songs");
        }

        internal override void RemoveSong(Song song)
        {
            _songs.Remove(song);
            song.PropertyChanged -= Song_PropertyChanged;

            RaisePropertyChanged("Songs");
        }

        private void Song_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            RaisePropertyChanged("Songs");
        }

        internal override void ClearSongVotes(string id)
        {
            var songs = _songs.Where(x => x.SongID == id);
            if(songs.Count() > 1)
                LogTo.Error("Songs in list should be unique");

            foreach (var song in songs)
            {
                // TODO Maybe Song should know where and how to publish itself / or hook into a service that can handle publishing changes
                song.Votes = 0;
                song.Voters = "";
            }
        }

        public ObservablePlaylistProvider(IMQTTSettings settings)
            : base(settings)
        {
            _songs = new ObservableCollection<Song>();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}