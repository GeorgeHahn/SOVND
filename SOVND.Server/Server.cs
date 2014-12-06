using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Charlotte;
using SpotifyClient;
using Anotar.NLog;
using SOVND.Lib.Handlers;
using SOVND.Lib.Models;
using SOVND.Server.Settings;
using Newtonsoft.Json;
using SOVND.Lib.Utils;
using StackExchange.Redis;

namespace SOVND.Server
{
    public class Server : MqttModule
    {
        private readonly ServerSpotifyAuth _spot;
        private readonly ConnectionMultiplexer _redisconnection;
        private Dictionary<string, ChannelHandler> channels = new Dictionary<string, ChannelHandler>();
        private IDatabase _redis;

        public new void Run()
        {
            Spotify.Initialize();
            if (!Spotify.Login("SOVND_server", _spot.Username, _spot.Password))
                throw new Exception("Spotify login failure");
            LogTo.Trace("Logged into Spotify");
            while (!Spotify.Ready())
                Thread.Sleep(100);
            Connect();
            LogTo.Debug("Connected to MQTT");
        }

        public Server(IMQTTSettings settings, IChannelHandlerFactory chf, ServerSpotifyAuth spot, RedisProvider redis)
            : base(settings.Broker, settings.Port, settings.Username, settings.Password)
        {
            _spot = spot;
            _redisconnection = redis.redis;
            _redis = _redisconnection.GetDatabase();
            LogTo.Trace("Initializing routes");

            //////
            // User messages
            //////
            /// 
            // Dirty hack to get libspotify to load songs
            On["/user/{username}/{channel}/songssearch/"] = _ =>
            {
                Search search = Spotify.GetSearch(_.Message);
                LogTo.Debug("SONG LOAD HACK: Searched \{_.Message} and \{(search.IsLoaded ? "is" : "is not")} loaded");
            };

            // Handle user-channel interaction
            On["/user/{username}/{channel}/songs/{songid}"] = msg =>
            {

                if (msg.Message == "vote")
                {
                    AddVote(msg.channel, msg.songid, msg.username);
                }
                else if (msg.Message == "unvote")
                {
                    RemoveVote(msg.channel, msg.songid, msg.username);
                }
                else if (msg.Message == "report")
                {
                    ReportSong(msg.channel, msg.songid, msg.username);
                }
                else if (msg.Message == "remove")
                {
                    RemoveSong(msg.channel, msg.songid, msg.username);
                }
                else if (msg.Message == "block")
                {
                    BlockSong(msg.channel, msg.songid, msg.username);
                }
                else
                {
                    LogTo.Warn("[{0}] Invalid command: {1}: {2}, by {3}", (string)msg.channel, (string)msg.Topic, (string)msg.Message, (string)msg.username);
                    return;
                }
            };

            // Handle channel registration
            On["/user/{username}/register/{channel}"] = _ =>
            {
                // TODO Separate channel name from ID

                Channel channel = JsonConvert.DeserializeObject<Channel>(_.Message);

                if (channel == null || 
                    string.IsNullOrWhiteSpace(channel.Name))
                {
                    LogTo.Warn("Rejected invalid channel JSON from {0} for channel {1}: {2}", (string)_.username, (string)_.channel, (string)_.Message);
                    return;
                }

                LogTo.Info("[{0}] \{_.username} sent channel data: {1}", (string)_.channel, (string)_.Message);

                if (!_redis.KeyExists(GetChannelNameID(_.channel)))
                {
                    // Channel doesn't exist yet
                    LogTo.Info("[{0}] Setting up new channel for {1}", (string)_.channel, (string)_.username);

                    _redis.StringSet(GetChannelNameID(_.channel), channel.Name);
                    _redis.StringSet(GetChannelDescriptionID(_.channel), channel.Description);
                    _redis.SetAdd(GetChannelModeratorID(_.channel), _.username);
                }
                else
                {
                    // Channel exists

                    if (!_redis.SetContains(GetChannelModeratorID(_.channel), _.username))
                    {
                        LogTo.Error("[{0}] User {1} not a moderator of channel", (string)_.channel, (string)_.username);
                        return;
                    }

                    LogTo.Info("[{0}] changing existing channel for {1}", (string)_.channel, (string)_.username);

                    _redis.StringSet(GetChannelNameID(_.channel), channel.Name);
                    _redis.StringSet(GetChannelDescriptionID(_.channel), channel.Description);
                }

                Publish("/\{_.channel}/info", _.Message, true);
            };

            // Handle user chat messages
            On["/user/{username}/{channel}/chat"] = _ =>
            {
                LogTo.Trace("\{_.channel}-> \{_.username}: \{_.Message}");
                
                // TODO [LOW] Log chats
                // TODO [LOW] Allow moderators to mute users

                if (channels.ContainsKey(_.channel))
                    Publish("/\{_.channel}/chat", "\{_.username}: \{_.Message}");
                else
                    LogTo.Debug("Chat was for invalid channel");
            };

            //////
            // Channel messages
            //////
            // TODO: This should be separate from above

            // Handle channel info
            On["/{channel}/info"] = _ =>
            {
                LogTo.Info("[{0}] Got info: \{_.Message}", (string)_.channel);

                Channel channel = JsonConvert.DeserializeObject<Channel>(_.Message);

                if (!channels.ContainsKey(_.channel))
                {
                    ChannelHandler channelHandler = chf.CreateChannelHandler(_.channel);
                    channelHandler.Subscribe();
                    channels[_.channel] = channelHandler;
                    StartChannelScheduler(channelHandler);
                }
            };
        }

        private RedisKey GetChannelModeratorID(string channelName)
        {
            return channelName + ".moderators";
        }

        private RedisKey GetChannelDescriptionID(string channelName)
        {
            return channelName + ".description";
        }

        private RedisKey GetChannelNameID(string channelName)
        {
            return channelName + ".name";
        }

        private void AddVote(string channel, string songid, string username)
        {
            LogTo.Debug("{0} voted for song {1}", username, songid);
            if (!channels.ContainsKey(channel))
            {
                LogTo.Warn("Got a vote from {0} for nonexistent channel: {1}", username, channel);
                return;
            }

            var playlist = channels[channel].Playlist; // TODO Nasty
            playlist.AddVote(songid, username);
        }

        private void RemoveVote(string channel, string songid, string username)
        {
            LogTo.Warn("Unvoting is currently disabled");
            return;

            //Log("\{username} unvoted for song \{songid}");

            //// TODO if songid is valid
            //if (uservotes[username + songid])
            //{
            //    votes[songid]--;
            //    uservotes[username + songid] = false;

            //    Publish("/\{channel}/playlist/\{songid}/votes", votes[songid].ToString());
            //}
            //else
            //{
            //    Log("Unvote was invalid");
            //    return;
            //}
        }

        private void ReportSong(string channel, string songID, string username)
        {
            LogTo.Debug("[{0}] \{username} reported song \{songID}", channel);

            // TODO Record that user reported song

            Publish("/\{channel}/playlist/\{songID}/reported", "true");
        }

        private void RemoveSong(string channel, string songID, string username)
        {
            LogTo.Debug("[{0}] \{username} removed song \{songID}", channel);

            // TODO Verify priveleges
            
            Publish("/\{channel}/playlist/\{songID}/votes", "", true);
            Publish("/\{channel}/playlist/\{songID}/votetime", "", true);
            Publish("/\{channel}/playlist/\{songID}/removed", "true");
        }

        private void BlockSong(string channel, string songID, string username)
        {
            LogTo.Debug("\{username} blocked song \{songID} on \{channel}");

            // TODO Verify priveleges
            // TODO Record block

            RemoveSong(channel, songID, username);
        }

        private Dictionary<ChannelHandler, bool> runningScheduler = new Dictionary<ChannelHandler, bool>();
        private static object schedulerlock = new object();

        // TODO: This should be refactored away from a thread-per-channel design, this won't scale well
        private void StartChannelScheduler(ChannelHandler channelHandler)
        {
            lock (schedulerlock)
            {
                if (runningScheduler.ContainsKey(channelHandler) && runningScheduler[channelHandler])
                {
                    LogTo.Debug("Already running scheduler for {0}", channelHandler.Name);
                    return;
                }
                else
                    runningScheduler[channelHandler] = true;
            }

            var task = Task.Factory.StartNew(() =>
            {
                var channel = channelHandler.Name;
                var playlist = (ISortedPlaylistProvider)channels[channel].Playlist;
                LogTo.Debug("[{0}] Starting track scheduler", channel);
                
                while (true)
                {
                    Song song = playlist.GetTopSong();
                    
                    if (song == null || song.track == null || song.track.Seconds == 0)
                    {
                        if(song != null)
                            song.track = new Track(song.SongID);
                        if (song == null)
                            LogTo.Trace("[{0}] No songs in channel", channel);
                        else
                        {
                            LogTo.Debug("[{1}] Skipping song: Either no track or no track time for track {0}", song.SongID, channel);
                            ClearSong(channelHandler, song);
                        }
                        Thread.Sleep(1000);
                        continue;
                    }
                    else
                    {
                        if (song.track?.Name != null)
                            LogTo.Debug("[{0}] Playing \{song.track.Name}", channel);

                        PlaySong(channelHandler, song.SongID);

                        var songtime = song.track.Seconds;
                        Thread.Sleep((int) Math.Ceiling(songtime*1000));

                        if (song.track?.Name != null)
                            LogTo.Trace("[{0}] Finished playing \{song.track.Name}", channel);

                        ClearSong(channelHandler, song);
                        continue;
                    }
                }
            });
        }

        private void PlaySong(ChannelHandler channel, string songid)
        {
            Publish("/\{channel.Name}/nowplaying/songid", "");
            Publish("/\{channel.Name}/nowplaying/songid", songid, true);
            Publish("/\{channel.Name}/nowplaying/starttime", Time.Timestamp().ToString(), true);
        }

        private void ClearSong(ChannelHandler channel, Song song)
        {
            // Set prev song to 0 votes, 0 vote time
            channel.ClearVotes(song.SongID);

            Publish("/\{channel.Name}/playlist/\{song.SongID}/votes", "0", true);
            Publish("/\{channel.Name}/playlist/\{song.SongID}/votetime", Time.Timestamp().ToString(), true);
            Publish("/\{channel.Name}/playlist/\{song.SongID}/voters", "", true);
        }
    }
}