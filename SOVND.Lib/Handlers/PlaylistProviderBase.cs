using System.Collections.Generic;
using Anotar.NLog;
using Charlotte;
using Newtonsoft.Json;
using SOVND.Lib.Models;

namespace SOVND.Lib.Handlers
{
    public abstract class PlaylistProviderBase : MqttModule, IPlaylistProvider
    {
        private ChannelHandler _channel;

        private Dictionary<string, int> votes = new Dictionary<string, int>();
        private Dictionary<string, bool> uservotes = new Dictionary<string, bool>();

        internal abstract void AddSong(Song song);
        internal abstract void RemoveSong(Song song);
        internal abstract void ClearSongVotes(string id);

        public bool AddVote(string songID, string username) // TODO: THIS DOES NOT BELONG IN THIS CLASS
        {
            if (!uservotes.ContainsKey(username + songID) || !uservotes[username + songID])
            {
                LogTo.Trace("Vote was valid");

                if (!votes.ContainsKey(songID))
                {
                    votes[songID] = 0;
                }

                votes[songID]++;
                uservotes[username + songID] = true;

                // TODO publish voter names
                return true;
            }
            else
            {
                LogTo.Error("Vote was invalid: {0} voted for {1}", username, songID);
                return false;
            }
        }

        public void ClearVotes(string songID)
        {
            ClearSongVotes(songID);

            votes[songID] = 0;

            // TODO this is nasty
            var keystoclear = new List<string>();
            foreach (var key in uservotes.Keys)
            {
                if (key.EndsWith(songID))
                    keystoclear.Add(key);
            }

            foreach(var key in keystoclear)
                uservotes[key] = false;
        }

        public int GetVotes(string songID)
        {
            return votes[songID];
        }

        private void AddNewSong(string ID)
        {
            var song = new Song(ID);

            // This means the song ID wasn't valid
            // TODO cleaner way
            if (song.SongID != ID)
                return;

            LogTo.Trace("Added song {0}" , ID);
            _channel.SongsByID[ID] = song;

            AddSong(song);
        }

        private void AddNewSong(Song song)
        {
            LogTo.Trace("Added song {0}", song.SongID);
            _channel.SongsByID[song.SongID] = song;

            AddSong(song);
        }

        public void Subscribe(ChannelHandler channel)
        {
            _channel = channel;

            // ChannelHandler playlists
            On["/" + _channel.Name + "/playlist/{songid}"] = _ =>
            {
                Song song;
                _channel.SongsByID.TryGetValue(_.songid, out song);

                if ((_.Message == "") || (_.Message == "remove"))
                {
                    // Remove song
                    if (song != null)
                    {
                        RemoveSong(song);
                        LogTo.Debug("[{0}] Removed song {1}", _channel.Name,
                            song.track.Loaded ? song.track.Name : song.SongID);
                        _channel.SongsByID.Remove(_.songid);
                    }
                    return;
                }

                SongModel newsong = JsonConvert.DeserializeObject<SongModel>(_.Message);

                if (song == null)
                {
                    song = new Song(newsong.SongID, true);
                    AddNewSong(song);
                }
                song.Voters = newsong.Voters;
                song.Votes = newsong.Votes;
                song.Votetime = newsong.Votetime;
                song.Removed = newsong.Removed;
                song.Playing = newsong.Playing;

                LogTo.Debug("[{0}] Song {1} modified: {2}", _channel.Name, song.track.Loaded ? song.track.Name : "", song.ToString());
            };
            Run();
        }

        public void ShutdownHandler()
        {
            Disconnect();
        }

        public PlaylistProviderBase(IMQTTSettings settings)
            : base(settings.Broker, settings.Port, settings.Username, settings.Password)
        { }
    }
}
