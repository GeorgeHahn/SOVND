using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
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
            // Dirty hack to get libspotify to load songs
            On["/user/{username}/{channel}/songssearch/"] = _ =>
            {
                //Search search = Spotify.GetSearch(_.Message);
                //LogTo.Debug("SONG LOAD HACK: Searched \{_.Message} and \{(search.IsLoaded ? "is" : "is not")} loaded");
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
                        LogTo.Error("[{0}] Error: User {1} not a moderator of channel", (string)_.channel, (string)_.username);
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
                
                // TODO [LOW] Allow moderators to mute users

                if (channels.ContainsKey(_.channel))
                {
                    var topic = string.Format("/{0}/chat", _.channel);
                    var message = string.Format("{0}: {1}", _.username, _.Message);
                    Publish(topic, message);
                    HipchatSender.SendNotification(_.channel, message, RoomColors.Gray);
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

            if (!_redis.SetContains(GetChannelModeratorID(channel), username))
            {
                LogTo.Error("[{0}] Error: User {1} not a moderator of channel", channel, username);
                return;
            }

            Publish("/\{channel}/playlist/\{songID}/votes", "", true);
            Publish("/\{channel}/playlist/\{songID}/votetime", "", true);
            Publish("/\{channel}/playlist/\{songID}/removed", "true");
        }

        private void BlockSong(string channel, string songID, string username)
        {
            LogTo.Debug("\{username} blocked song \{songID} on \{channel}");

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

        private async void StartChannelScheduler(ChannelHandler channelHandler)
        {
            lock (schedulerlock)
            {
                bool value;
                if (runningScheduler.TryGetValue(channelHandler, out value) && value)
                {
                    LogTo.Debug("Already running scheduler for {0}", channelHandler.Name);
                    return;
                }
                else
                    runningScheduler[channelHandler] = true;
            }

            var channel = channelHandler.Name;
            var playlist = (ISortedPlaylistProvider) channels[channel].Playlist;
            LogTo.Debug("[{0}] Starting track scheduler", channel);

            while (true)
            {
                Song song = playlist.GetTopSong();

                if (song == null || song.track == null || song.track.Seconds == 0)
                {
                    if (song == null)
                    {
                        HipchatSender.SendNotification(channel, "No songs in channel, waiting for a song",
                            RoomColors.Red);
                        while (playlist.GetTopSong() == null)
                        {
                            await Task.Delay(1000);
                        }
                        HipchatSender.SendNotification(channel, "Got a song", RoomColors.Green);
                    }
                    else
                    {
                        if (song.track == null)
                            song.track = new Track(song.SongID);

                        if (playlist.Songs.Count == 1)
                        {
                            HipchatSender.SendNotification(channel,
                                string.Format("Only one song in channel, waiting for it to load: {0}", song.SongID),
                                RoomColors.Red);
                            while ((!song.track.Loaded) && playlist.Songs.Count == 1)
                            {
                                await Task.Delay(1000);
                            }
                            HipchatSender.SendNotification(channel, "Song loaded or another was added", RoomColors.Green);
                        }
                        else
                        {
                            HipchatSender.SendNotification(channel,
                                string.Format("Skipping song: Either no track or no track time for track {0}",
                                    song.SongID), RoomColors.Red);
                            ClearSong(channelHandler, song);
                        }
                    }
                    await Task.Delay(1000);
                    continue;
                }
                else
                {
                    PlaySong(channelHandler, song);

                    var songtime = song.track.Seconds;
                    await Task.Delay((int) Math.Ceiling(songtime*1000));

                    ClearSong(channelHandler, song);
                    continue;
                }
            }

        }

        private void PlaySong(ChannelHandler channel, Song song)
        {
            HipchatSender.SendNotification(channel.Name, "Playing song: " + (song.track.Loaded ? song.track.Name : song.SongID), RoomColors.Green);

            Publish("/\{channel.Name}/nowplaying/songid", "");
            Publish("/\{channel.Name}/nowplaying/songid", song.SongID, true);
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