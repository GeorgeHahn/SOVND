using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Anotar.NLog;
using SOVND.Lib.Models;

namespace SOVND.Lib.Handlers
{
    public class ObservablePlaylistProvider : PlaylistProviderBase, IObservablePlaylistProvider
    {
        private readonly SyncHolder _sync;
        private readonly ObservableCollection<Song> _songs;

        public ObservableCollection<Song> Songs
        {
            get { return _songs; }
        }

        internal override void AddSong(Song song)
        {
            if (_sync.sync != null)
                _sync.sync.Send((x) => _songs.Add(song), null); // TODO Bad bad bad bad
            else
                _songs.Add(song);

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

        public ObservablePlaylistProvider(IMQTTSettings settings, SyncHolder sync)
            : base(settings)
        {
            _sync = sync;
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