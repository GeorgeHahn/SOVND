using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace SOVND.Lib
{
    public class Channel
    {
        public Channel(string mqttname)
        {
            MQTTName = mqttname;
        }

        public string MQTTName { get; private set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string Image { get; set; }

        // TODO Moderators

        public Dictionary<string, Song> SongsByID { get; set; } = new Dictionary<string, Song>();

        public List<Song> Songs { get; set; } = new List<Song>();

        public PlaylistProvider _playlist { get; private set; } // TODO this should be private with important parts exposed via properties

        private ChatProvider _chat;

        public ObservableCollection<ChatMessage> Chats
        {
            get { return _chat?.Chats; }
        }

        public void Subscribe()
        {
            // Don't double subscribe
            if (_playlist == null)
                _playlist = new PlaylistProvider(this);
            if (_chat == null)
                _chat = new ChatProvider(this);
        }

        public void Unsubscribe()
        {
            _playlist.Disconnect();
            _playlist = null;
        }

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