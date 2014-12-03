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

namespace SOVND.Server
{
    public class Server : MqttModule
    {
        private readonly ServerSpotifyAuth _spot;
        private Dictionary<string, ChannelHandler> channels = new Dictionary<string, ChannelHandler>();

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

        public Server(IMQTTSettings settings, IChannelHandlerFactory chf, ServerSpotifyAuth spot)
            : base(settings.Broker, settings.Port, settings.Username, settings.Password)
        {
            _spot = spot;
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
                    LogTo.Warn("Invalid command: {0}: {1}, by {2}", (string)msg.Topic, (string)msg.Message, (string)msg.username);
                    return;
                }
            };

            // Handle channel registration
            On["/user/{username}/register/{channel}"] = _ =>
            {
                // TODO Verify that channel hasn't already been created
                // TODO Check user permissions
                // TODO Publish _.username as moderater
                // TODO Force channel ID to lowercase
                // TODO Separate channel name from ID

                Channel channel = JsonConvert.DeserializeObject<Channel>(_.Message);

                if (channel == null || 
                    string.IsNullOrWhiteSpace(channel.Name))
                {
                    LogTo.Warn("Rejected invalid channel JSON from {0} for channel {1}: {2}", (string)_.username, (string)_.channel, (string)_.Message);
                    return;
                }

                LogTo.Info("\{_.username} sent channel data for {0}: {1}", (string)_.channel, (string)_.Message);

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
                LogTo.Info("\{_.channel} got info: \{_.Message}");

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

        private void AddVote(string channel, string songid, string username)
        {
            LogTo.Debug("\{username} voted for song \{songid}");
            if (!channels.ContainsKey(channel))
            {
                LogTo.Warn("Got a vote from \{username} for nonexistent channel: \{channel}");
                return;
            }

            var playlist = channels[channel].Playlist; // TODO Nasty

            if (playlist.AddVote(songid, username))
            {
                Publish("/\{channel}/playlist/\{songid}/votes", playlist.GetVotes(songid).ToString(), true);
                Publish("/\{channel}/playlist/\{songid}/votetime", Time.Timestamp().ToString(), true);
            }
        }

        private void RemoveVote(string channel, string songid, string username)
        {
            LogTo.Warn("Unvoting currently disabled");
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
            LogTo.Debug("\{username} reported song \{songID} on \{channel}");

            LogTo.Warn("Song reporting is currently disabled");

            // TODO Record that user reported song

            Publish("/\{channel}/playlist/\{songID}/reported", "true");
        }

        private void RemoveSong(string channel, string songID, string username)
        {
            LogTo.Debug("\{username} removed song \{songID} on \{channel}");

            // TODO Verify priveleges
            
            Publish("/\{channel}/playlist/\{songID}/votes", "", true);
            Publish("/\{channel}/playlist/\{songID}/votetime", "");
            Publish("/\{channel}/playlist/\{songID}/removed", "true", true);
        }

        private void BlockSong(string channel, string songID, string username)
        {
            LogTo.Debug("\{username} blocked song \{songID} on \{channel}");

            // TODO Verify priveleges
            // TODO Record block

            Publish("/\{channel}/playlist/\{songID}/votes", "");
            Publish("/\{channel}/playlist/\{songID}/votetime", "");
            Publish("/\{channel}/playlist/\{songID}/removed", "true");
            Publish("/\{channel}/playlist/\{songID}/blocked", "true");
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
                LogTo.Debug("Starting track scheduler for \{channel}");
                
                while (true)
                {
                    Song song = playlist.GetTopSong();
                    
                    if (song == null || song.track == null || song.track.Seconds == 0)
                    {
                        if(song != null)
                            song.track = new Track(song.SongID);
                        if (song == null)
                            LogTo.Trace("No songs in channel: {0}", channel);
                        else
                            LogTo.Debug("Either no track or no track time for track {0} in channel {1}", song.SongID, channel);
                        Thread.Sleep(1000);
                        continue;
                    }
                    else
                    {
                        if (song.track?.Name != null)
                            LogTo.Debug("Playing \{song.track.Name}");

                        Publish("/\{channel}/nowplaying/songid", "", true);
                        Publish("/\{channel}/nowplaying/songid", song.SongID, true);
                        Publish("/\{channel}/nowplaying/starttime", Time.Timestamp().ToString(), true);

                        var songtime = song.track.Seconds;
                        Thread.Sleep((int) Math.Ceiling(songtime*1000));

                        if (song.track?.Name != null)
                            LogTo.Trace("Finished playing \{song.track.Name}");

                        // Set prev song to 0 votes, 0 vote time
                        channelHandler.ClearVotes(song.SongID);

                        Publish("/\{channel}/playlist/\{song.SongID}/votes", "0", true);
                        Publish("/\{channel}/playlist/\{song.SongID}/votetime", Time.Timestamp().ToString(), true);
                        Publish("/\{channel}/playlist/\{song.SongID}/voters", "", true);
                        continue;
                    }
                }
            });
        }
    }
}