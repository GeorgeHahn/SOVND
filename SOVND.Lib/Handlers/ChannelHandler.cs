using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SOVND.Lib.Models;

namespace SOVND.Lib.Handlers
{
    public class ChannelHandler
    {
        private readonly IChatProviderFactory _chatProviderFactory;

        public ChannelHandler(IPlaylistProvider playlistProvider, IChatProviderFactory chatProviderFactory, string name)
        {
            _chatProviderFactory = chatProviderFactory;
            Playlist = playlistProvider;
            
            Name = name;
        }

        private Channel thisChannel = new Channel();

        public string Name
        {
            get { return thisChannel.Name; }
            private set { thisChannel.Name = value; }
        }

        public string Description
        {
            get { return thisChannel.Description; }
            set { thisChannel.Description = value; }
        }

        public string Image { get; set; }

        // TODO Moderators

        public Dictionary<string, Song> SongsByID { get; set; } = new Dictionary<string, Song>();

        public IPlaylistProvider Playlist { get; private set; } // TODO this should be private with important parts exposed via properties

        private IChatProvider _chat;

        public ObservableCollection<ChatMessage> Chats
        {
            get
            {
                if(_chat != null)
                    return _chat.Chats;
                return null;
            }
        }

        public void Subscribe()
        {
            Playlist.Subscribe(this);
            _chat = _chatProviderFactory.CreateChatProvider(thisChannel);
        }

        public void ShutdownHandler()
        {
            if (Playlist != null)
            {
                Playlist.ShutdownHandler();
                Playlist = null;
            }

            if (_chat != null)
            {
                _chat.ShutdownHandler();
                _chat = null;
            }
        }

        public void ClearVotes(string songID)
        {
            Playlist.ClearVotes(songID);
        }
        // TODO Return an enum that counts down from the top of the list (write a sorting function and use C#'s sorting)
    }
}