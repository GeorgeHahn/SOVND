using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Charlotte;
using SpotifyClient;
using Anotar.NLog;
using HipchatApiV2.Enums;
using SOVND.Lib.Handlers;
using SOVND.Lib.Models;
using SOVND.Server.Settings;
using Newtonsoft.Json;
using SOVND.Lib.Utils;
using SOVND.Server.Handlers;
using SOVND.Server.Utils;
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
            
            // Handle user-channel interaction
            On["/user/{username}/{channel}/songs/{songid}"] = msg =>
            {
                switch ((string)msg.Message)
                {
                case "vote":
                    AddVote(msg.channel, msg.songid, msg.username);
                    break;
                case "unvote":
                    RemoveVote(msg.channel, msg.songid, msg.username);
                    break;
                case "report":
                    ReportSong(msg.channel, msg.songid, msg.username);
                    break;
                case "remove":
                    RemoveSong(msg.channel, msg.songid, msg.username);
                    break;
                case "block":
                    BlockSong(msg.channel, msg.songid, msg.username);
                    break;

                default:
                    LogTo.Warn("[{0}] Invalid command: {1}: {2}, by {3}", (string)msg.channel, (string)msg.Topic, (string)msg.Message, (string)msg.username);
                    break;
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

                LogTo.Info("[{0}] {1} sent channel data: {2}", (string)_.channel, (string) _.username, (string)_.Message);

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
                        LogTo.Error("[{0}] Error: User {1} not a moderator of channel", (string)_.channel, (string)_.username);
                        return;
                    }

                    LogTo.Info("[{0}] changing existing channel for {1}", (string)_.channel, (string)_.username);

                    _redis.StringSet(GetChannelNameID(_.channel), channel.Name);
                    _redis.StringSet(GetChannelDescriptionID(_.channel), channel.Description);
                }

                Publish(string.Format("/{0}/info", _.channel), _.Message, true);
            };

            // Handle user chat messages
            On["/user/{username}/{channel}/chat"] = _ =>
            {
                LogTo.Trace("{0}->{1}: {2}", (string)_.username, (string)_.channel, (string)_.Message);

                // TODO [LOW] Allow moderators to mute users

                if (channels.ContainsKey(_.channel))
                {
                    var topic = string.Format("/{0}/chat", _.channel);
                    var message = new ChatMessage
                    {
                        message = _.Message,
                        username = _.username,
                        time = Time.Timestamp()
                    };
                    var messageJson = JsonConvert.SerializeObject(message);
                    Publish(topic, messageJson);
                    HipchatSender.SendNotification(_.channel, string.Format("{0}: {1}", message.username, message.message), RoomColors.Gray);
                }
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
                LogTo.Info("[{0}] Got info: {1}", (string)_.channel, (string)_.Message);

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
            LogTo.Debug("[{0}] {1} voted for song {2}", channel, username, songid);
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
            LogTo.Debug("[{0}] {1} reported song {2}", channel, username, songID);

            // TODO Record report
        }

        private void RemoveSong(string channel, string songID, string username)
        {
            LogTo.Debug("[{0}] {1} removed song {2}", channel, username, songID);

            //if (!_redis.SetContains(GetChannelModeratorID(channel), username))
            //{
            //    LogTo.Error("[{0}] Error: User {1} not a moderator of channel", channel, username);
            //    return;
            //}

            CancellationTokenSource value;
            if (tokens.TryGetValue(channel + songID, out value))
                value.Cancel();

            Publish(string.Format("/{0}/playlist/{1}", channel, songID), "remove", true);
            Publish(string.Format("/{0}/nowplaying", channel), "remove", true);
            Publish(string.Format("/{0}//playlist/{1}", channel, songID), "", true);
            Publish(string.Format("/{0}/nowplaying", channel), "", true);
        }

        private void BlockSong(string channel, string songID, string username)
        {
            LogTo.Debug("{0} blocked song {1} on {2}", username, songID, channel);

            // TODO Record block

            if (!_redis.SetContains(GetChannelModeratorID(channel), username))
            {
                LogTo.Error("[{0}] Error: User {1} not a moderator of channel", channel, username);
                return;
            }

            RemoveSong(channel, songID, username);
        }

        private Dictionary<ChannelHandler, bool> runningScheduler = new Dictionary<ChannelHandler, bool>();
        private static object schedulerlock = new object();
        private Dictionary<string, CancellationTokenSource> tokens = new Dictionary<string, CancellationTokenSource>(); 

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

                runningScheduler[channelHandler] = true;
            }

            var channel = channelHandler.Name;
            var playlist = (ISortedPlaylistProvider)channels[channel].Playlist;
            LogTo.Debug("[{0}] Starting track scheduler", channel);

            Task.Run(() =>
            {
                while (true)
                    OneLoop(channelHandler, playlist, channel);
            });
        }

        private void OneLoop(ChannelHandler channelHandler, ISortedPlaylistProvider playlist, string channel)
        {
            Song song = playlist.GetTopSong();

            if (song == null)
            {
                LogTo.Debug("[{0}] No songs in channel, waiting for a song", channel);

                while (playlist.GetTopSong() == null)
                    Thread.Sleep(1000);

                LogTo.Debug("[{0}] Got a song", channel);
                return;
            }

            if (!song.track.Loaded)
            {
                if (playlist.Songs.Count == 1)
                {
                    LogTo.Warn("[{0}] Only one song in channel, waiting for it to load: {1}", channel, song.SongID);
                    song.track.onLoad = () => { LogTo.Debug("Got it!"); };
                    while ((!song.track.Loaded) && playlist.Songs.Count == 1)
                    {
                        Thread.Sleep(1000);
                    }
                    LogTo.Debug("[{0}] Song loaded or another was added", channel);
                    return;
                }

                LogTo.Warn("[{0}] Skipping song: track not loaded {1}", channel, song.SongID);
                ClearSong(channelHandler, song);
                Thread.Sleep(1000);
                return;
            }

            CancellationTokenSource token = new CancellationTokenSource();
            tokens[channelHandler.Name + song.SongID] = token;

            var songtime = song.track.Seconds;
            PlaySong(channelHandler, song);

            var time = DateTime.Now.AddMilliseconds((int)Math.Ceiling(songtime * 1000));
            while ((DateTime.Now < time) && !token.IsCancellationRequested)
                Thread.Sleep(100);

            ClearSong(channelHandler, song);

            tokens.Remove(channelHandler.Name + song.SongID);
            token.Dispose();
        }

        private void PlaySong(ChannelHandler channel, Song song)
        {
            LogTo.Debug("[{0}] Playing song {1}", channel.Name, song.track.Name);

            var nowplaying = new NowPlaying {songID = song.SongID, votetime = Time.Timestamp()};
            Publish(string.Format("/{0}/nowplaying", channel.Name), JsonConvert.SerializeObject(nowplaying), true);
        }

        private void ClearSong(ChannelHandler channel, Song song)
        {
            LogTo.Debug("[{0}] Clearing song {1}", channel.Name, song.track.Name);

            CancellationTokenSource value;
            if (tokens.TryGetValue(channel.Name + song.SongID, out value))
            {
                tokens.Remove(channel.Name + song.SongID);

                if (value.IsCancellationRequested)
                    return;
            }

            channel.ClearVotes(song.SongID);
            var model = new SongModel()
            {
                SongID = song.SongID,
                Votes = 0,
                Voters = "",
                Votetime = Time.Timestamp()
            };

            Publish(string.Format("/{0}/playlist/{1}", channel.Name, song.SongID), JsonConvert.SerializeObject(model), true);
        }
    }

    public static class TaskX
    {
        public static ConfiguredTaskAwaitable X(this Task task)
        {
            return task.ConfigureAwait(false);
        }
    }
}