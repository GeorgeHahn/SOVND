namespace SOVND.Lib.Models
{
    public class SongModel
    {
        public string SongID { get; set; }
        public string Voters { get; set; }
        public int Votes { get; set; }
        public long Votetime { get; set; }
        public bool Removed { get; set; }
    }
}