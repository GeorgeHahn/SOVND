using SpotifyClient;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace SOVND.Lib
{
    public class Song : IComparable, INotifyPropertyChanged
    {
        public Action<string> Log = _ => Console.WriteLine(_);
        private int _votes;

        public Song(string songID)
        {
            SongID = songID;

            track = new Track(songID);
        }

        public string SongID { get; private set; }
        public long Votetime { get; set; }

        public int Votes
        {
            get { return _votes; }
            set
            {
                _votes = value;
                RaisePropertyChanged(nameof(Votes));
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