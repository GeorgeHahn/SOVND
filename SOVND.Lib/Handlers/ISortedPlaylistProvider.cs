using System.Collections.Generic;
using SOVND.Lib.Models;

namespace SOVND.Lib.Handlers
{
    public interface ISortedPlaylistProvider : IPlaylistProvider
    {
        List<Song> Songs { get; }
        Song GetTopSong();
    }
}