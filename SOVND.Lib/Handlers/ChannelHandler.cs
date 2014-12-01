using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SOVND.Lib.Models;

namespace SOVND.Lib.Handlers
{
    public class ChannelHandler
    {
        public ChannelHandler(IPlaylistProvider playlistProvider, IChatProvider chatProvider, string MQTTName)
        {
            _playlist = playlistProvider;
            _chat = chatProvider;
            this.MQTTName = MQTTName;
        }

        public string MQTTName { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string Image { get; set; }

        // TODO Moderators

        public Dictionary<string, Song> SongsByID { get; set; } = new Dictionary<string, Song>();

        public ObservableCollection<Song> Songs
        {
            get { return _playlist?.Songs; }
        }

        public IPlaylistProvider _playlist { get; private set; } // TODO this should be private with important parts exposed via properties

        private IChatProvider _chat;

        public ObservableCollection<ChatMessage> Chats
        {
            get { return _chat?.Chats; }
        }

        public void Subscribe()
        {
            _playlist.Subscribe(this);
            _chat.Subscribe(this);
        }

        public void Unsubscribe()
        {
            _playlist.Unsubscribe();
            _playlist = null;
        }

        /// <summary>
        /// Gets the song at the top of the list
        /// </summary>
        /// <returns></returns>
        public Song GetTopSong()
        {
            if (Songs.Count == 0)
                return null;

            //Songs.Sort();
            return Songs[0];
        }

        public void ClearVotes(string songID)
        {
            _playlist.ClearVotes(songID);

            var keystoclear = Songs.Where((x) => x.SongID == songID);

            foreach (var key in keystoclear)
                key.Votes = 0;
        }

        // TODO Return an enum that counts down from the top of the list (write a sorting function and use C#'s sorting)
    }

    public interface IChannelHandlerFactory
    {
        ChannelHandler CreateChannelHandler(string MQTTName);
    }
}