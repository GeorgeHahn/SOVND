using System;
using System.ComponentModel;
using SpotifyClient;
using Anotar.NLog;

namespace SOVND.Lib.Models
{
    public class Song : IComparable, INotifyPropertyChanged
    {
        private int _votes;
        private long _votetime;

        public Song(string songID)
        {
            SongID = songID;

            // TODO Super nasty, need to decouple Song and Track
            if (songID == "test")
                return;

            track = new Track(songID);
            track.onLoad = () => LogTo.Info("Loaded {0}: {1}", track.SongID, track.Name);
        }

        public string SongID { get; private set; }

        public long Votetime
        {
            get { return _votetime; }
            set
            {
                _votetime = value;
                RaisePropertyChanged("Votetime");
            }
        }

        public int Votes
        {
            get { return _votes; }
            set
            {
                _votes = value;
                RaisePropertyChanged("Votes");
            }
        }

        public string Voters { get; set; }
        public bool Removed { get; set; }
        public Track track { get; set; }

        public int CompareTo(object obj)
        {
            var two = obj as Song;
            if (two == null)
                throw new ArgumentException("Obj is not Song");

            if (Votes > two.Votes)
                return -1;
            if (two.Votes > Votes)
                return 1;
            if (Votetime < two.Votetime)
                return -1;
            if (two.Votetime < Votetime)
                return 1;
            return 0;
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