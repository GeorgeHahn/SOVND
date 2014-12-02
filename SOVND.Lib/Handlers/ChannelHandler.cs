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
            Playlist = playlistProvider;
            _chat = chatProvider;
            this.MQTTName = MQTTName;
        }

        public string MQTTName { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string Image { get; set; }

        // TODO Moderators

        public Dictionary<string, Song> SongsByID { get; set; } = new Dictionary<string, Song>();

        public IPlaylistProvider Playlist { get; private set; } // TODO this should be private with important parts exposed via properties

        private IChatProvider _chat;

        public ObservableCollection<ChatMessage> Chats
        {
            get { return _chat?.Chats; }
        }

        public void Subscribe()
        {
            Playlist.Subscribe(this);
            _chat.Subscribe(this);
        }

        public void Unsubscribe()
        {
            Playlist.Unsubscribe();
            Playlist = null;
        }

        public void ClearVotes(string songID)
        {
            Playlist.ClearVotes(songID);
        }
        // TODO Return an enum that counts down from the top of the list (write a sorting function and use C#'s sorting)
    }
}