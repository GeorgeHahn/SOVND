using System;
using System.ComponentModel;
using Anotar.NLog;
using PropertyChanged;
using SpotifyClient;

namespace SOVND.Lib.Models
{
    [ImplementPropertyChanged]
    public class Song : IComparable, INotifyPropertyChanged
    {
        private int _votes;
        private long _votetime;

        public Song(string songID, bool fetchAlbumArt = true)
        {
            SongID = songID;

            // TODO Super nasty, need to decouple Song and Track
            if (songID == "test")
                return;

            track = new Track(songID, fetchAlbumArt);
            track.onLoad = () => LogTo.Info("Loaded {0}: {1}", track.SongID, track.Name);
            track.PropertyChanged += (sender, args) =>
            {
                if(PropertyChanged != null)
                    PropertyChanged(null, new PropertyChangedEventArgs("track." + args.PropertyName));
            };
        }

        public Song()
        {
            
        }

        public string SongID { get; private set; }
        public long Votetime { get; set; }
        public int Votes { get; set; }
        public string Voters { get; set; }
        public bool Removed { get; set; }
        public Track track { get; set; }
        public bool Playing { get; set; }

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

        public override string ToString()
        {
            return string.Format("ID: {0}, Votes: {1}, Votetime: {2}, Voters: {3}, Playing: {4}", SongID, Votes, Votetime, Voters, Playing);
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}