using System.Collections.Generic;
using System.Linq;
using Anotar.NLog;
using Charlotte;
using Newtonsoft.Json;
using SOVND.Lib.Handlers;
using SOVND.Lib.Models;
using SOVND.Lib.Utils;
using SOVND.Server.Settings;
using StackExchange.Redis;

namespace SOVND.Server.Handlers
{
    public class SortedPlaylistProvider : MqttModule, IPlaylistProvider, ISortedPlaylistProvider
    {
        private readonly IDatabase _redis;
        private ChannelHandler _channel;
        private string chname;

        private string GetVotesID(string songID)
        {
            return chname + songID + ".votes";
        }

        private string GetVotersID(string songID)
        {
            return chname + songID + ".voters";
        }

        private string GetAdderID(string songID)
        {
            return chname + songID + ".adder";
        }

        private string GetAddTimeID(string songID)
        {
            return chname + songID + ".addtime";
        }

        public bool AddVote(string songID, string username) // TODO: THIS DOES NOT BELONG IN THIS CLASS
        {
            var song_voters = GetVotersID(songID);
            var song_votes = GetVotesID(songID);

            if (!_redis.SetContains(song_voters, username))
            {
                LogTo.Trace("Vote was valid");

                if (song_votes == "0")
                {
                    _redis.StringSet(GetAdderID(songID), username);
                    _redis.StringSet(GetAddTimeID(songID), Time.Timestamp());
                }

                _redis.StringIncrement(song_votes);
                _redis.SetAdd(song_voters, username);

                var newsong = new SongModel
                {
                    SongID = songID,
                    Votes = int.Parse(_redis.StringGet(song_votes)),
                    Voters = string.Join(",", _redis.SetMembers(song_voters).ToStringArray()),
                    Votetime = Time.Timestamp()
                };
                Publish("/\{_channel.Name}/playlist/\{songID}", JsonConvert.SerializeObject(newsong), true);

                // TODO publish voter names
                return true;
            }
            else
            {
                LogTo.Error("[{0}] Vote was invalid: {1} voted for {2}", _channel.Name, username, songID);
                return false;
            }
        }

        public void ClearVotes(string songID)
        {
            var song_voters = GetVotersID(songID);
            var song_votes = GetVotesID(songID);

            ClearSongVotes(songID);

            _redis.StringSet(song_votes, 0);
            _redis.KeyDelete(song_voters);
        }

        public int GetVotes(string songID)
        {
            return int.Parse(_redis.StringGet(GetVotesID(songID)));
        }

        private void AddNewSong(string ID)
        {
            var song = new Song(ID, false);

            // This means the song ID wasn't valid
            // TODO cleaner way
            if (song.SongID != ID)
                return;

            AddNewSong(song);
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
            chname = _channel.Name + ".";

            On["/" + _channel.Name + "/playlist/{songid}"] = _ =>
            {
                Song song;
                _channel.SongsByID.TryGetValue(_.songid, out song);

                if (_.Message == "")
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
                if (newsong == null)
                {
                    LogTo.Debug("[{0}] Error: newsong is null");
                    return;
                }

                if (song == null)
                {
                    AddNewSong(newsong.SongID);
                    song = _channel.SongsByID[_.songid];
                }
                else
                {
                    song.Voters = newsong.Voters;
                    song.Votes = newsong.Votes;
                    song.Votetime = newsong.Votetime;
                    song.Removed = newsong.Removed;
                }

                LogTo.Debug("[{0}] Song {1} modified: {2}", _channel.Name, song.track.Loaded ? song.track.Name : song.SongID, song.ToString());
            };
            Run();
        }

        public void ShutdownHandler()
        {
            Disconnect();
        }

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

            // TODO Ability to toggle verbose debugging like this at runtime
            //var first = Songs[0].Votetime;
            //for (int i = 0; i < Songs.Count; i++)
            //    LogTo.Debug("Song {0}: {1} has {2} votes at {3} (o {4})", i, Songs[i].track?.Name, Songs[i].Votes, Songs[i].Votetime, Songs[i].Votetime - first);

            return Songs[0];
        }

        internal void AddSong(Song song)
        {
            // TODO Should intelligently insert songs
            Songs.Add(song);
            Songs.Sort();
        }

        internal void RemoveSong(Song song)
        {
            Songs.Remove(song);
        }

        internal void ClearSongVotes(string id)
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

        public SortedPlaylistProvider(IMQTTSettings settings, RedisProvider redis)
            : base(settings.Broker, settings.Port, settings.Username, settings.Password)
        {
            _redis = redis.redis.GetDatabase();
            Songs = new List<Song>();
        }
    }
}