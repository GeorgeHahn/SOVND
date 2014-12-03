using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Anotar.NLog;
using Charlotte;
using SOVND.Lib.Models;
using System;

namespace SOVND.Lib.Handlers
{
    public abstract class PlaylistProvider : MqttModule, IPlaylistProvider
    {
        private ChannelHandler _channel;

        private Dictionary<string, int> votes = new Dictionary<string, int>();
        private Dictionary<string, bool> uservotes = new Dictionary<string, bool>();

        internal abstract void AddSong(Song song);
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

            LogTo.Trace("Added song \{ID}");
            _channel.SongsByID[ID] = song;

            AddSong(song);
        }

        public void Subscribe(ChannelHandler channel)
        {
            _channel = channel;

            // ChannelHandler playlists
            On["/\{_channel.Name}/playlist/{songid}/votes"] = _ =>
            {
                LogTo.Debug("\{_channel.Name} got a vote for \{_.songid}");

                if (!channel.SongsByID.ContainsKey(_.songid))
                    AddNewSong(_.songid);
                var song = channel.SongsByID[_.songid];

                song.Votes = int.Parse(_.Message);
            };

            On["/\{_channel.Name}/playlist/{songid}/votetime"] = _ =>
            {
                if (!channel.SongsByID.ContainsKey(_.songid))
                    AddNewSong(_.songid);
                var song = channel.SongsByID[_.songid];
                song.Votetime = long.Parse(_.Message);
            };

            Run();
        }

        public void ShutdownHandler()
        {
            Disconnect();
            _channel = null;

            // This instance is no longer useful
            votes = null;
            uservotes = null;
        }

        public PlaylistProvider(IMQTTSettings settings)
            : base(settings.Broker, settings.Port, settings.Username, settings.Password)
        { }
    }
}
