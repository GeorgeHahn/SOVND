using System.Collections.Generic;
using System.Linq;

namespace SOVND.Server
{
    public class Channel
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public string Image { get; set; }

        // TODO Moderators

        public Dictionary<string, Song> SongsByID { get; set; } = new Dictionary<string, Song>();

        /// <summary>
        /// Gets the song at the top of the list
        /// </summary>
        /// <returns></returns>
        public Song GetTopSong()
        {
            Song max = SongsByID.Values.FirstOrDefault();
            foreach (var song in SongsByID.Values)
            {
                if (song == max)
                    continue;

                if (song.Votes > max.Votes)
                {
                    max = song;
                } else if (song.Votes == max.Votes && song.Votetime < max.Votetime)
                {
                    max = song;
                }
            }
            return max;
        }

        /// TODO Return an enum that counts down from the top of the list (write a sorting function and use C#'s sorting)
    }
}