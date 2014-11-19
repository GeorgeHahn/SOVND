using SpotifyClient;

namespace SOVND.Lib
{
    public class Song
    {
        public string SongID { get; set; }
        public long Votetime { get; set; }
        public int Votes { get; set; }
        public string Voters { get; set; } // ?
        public bool Removed { get; set; }
        public Track track { get; set; }
    }
}