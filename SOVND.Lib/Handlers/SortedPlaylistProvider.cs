using System;
using System.Collections.Generic;
using SOVND.Lib.Models;
using System.Linq;
using Anotar.NLog;
using SOVND.Lib.Utils;

namespace SOVND.Lib.Handlers
{
    public class SortedPlaylistProvider : PlaylistProviderBase, ISortedPlaylistProvider
    {
        public List<Song> Songs { get; private set; }

        /// <summary>
        /// Gets the song at the top of the list
        /// </summary>
        /// <returns></returns>
        public Song GetTopSong()
        {
            if (Songs.Count == 0)
                return null;

            Songs.Sort();

            var first = Songs[0].Votetime;
            for (int i = 0; i < Songs.Count; i++)
                LogTo.Debug("Song {0}: {1} has {2} votes at {3} (o {4})", i, Songs[i].track?.Name, Songs[i].Votes, Songs[i].Votetime, Songs[i].Votetime - first);

            return Songs[0];
        }

        internal override void AddSong(Song song)
        {
            // TODO Should intelligently insert songs
            Songs.Add(song);
            Songs.Sort();
        }

        internal override void ClearSongVotes(string id)
        {
            var songs = Songs.Where(x => x.SongID == id);
            if (songs.Count() > 1)
                LogTo.Error("Songs in list should be unique");

            var song = songs.First();

            // TODO Maybe Song should know where and how to publish itself / or hook into a service that can handle publishing changes
            song.Votes = 0;
            song.Voters = "";
            song.Votetime = Time.Timestamp();

            Songs.Sort();
        }

        public SortedPlaylistProvider(IMQTTSettings settings)
            : base(settings)
        {
            Songs = new List<Song>();
        }
    }
}