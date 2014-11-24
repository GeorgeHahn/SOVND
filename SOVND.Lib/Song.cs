using SpotifyClient;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SOVND.Lib
{
    public class Song : IComparable
    {
        public Action<string> Log = _ => Console.WriteLine(_);

        public Song(string songID)
        {
            SongID = songID;

            Spotify.InvokeOnSpotifyThread(() =>
            {
                Console.WriteLine("Creating new track");
                track = new Track(songID);
            });
        }

        public string SongID { get; private set; }
        public long Votetime { get; set; }
        public int Votes { get; set; }
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
    }
}