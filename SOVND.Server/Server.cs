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

namespace SOVND.Server
{
    public class Server : MqttModule
    {
        private readonly ServerSpotifyAuth _spot;
        private Dictionary<string, ChannelHandler> channels = new Dictionary<string, ChannelHandler>();

        public Server(IMQTTSettings settings, IChannelHandlerFactory chf, ServerSpotifyAuth spot)
            : base(settings.Broker, settings.Port, settings.Username, settings.Password)
        {
            _spot = spot;
            LogTo.Trace("Starting up");

            On["#"] = _ =>
            {
                LogTo.Trace("\{_.Topic}: \{_.Message}");
            };


            On["/user/{username}/{channel}/songssearch/"] = _ =>
            {
                Search search = Spotify.GetSearch(_.Message);
                LogTo.Debug("Searched \{_.Message} and \{(search.IsLoaded ? "is" : "is not")} loaded");
            };

            On["/user/{username}/{channel}/songs/{songid}"] = _ =>
            {
                if (_.Message == "vote")
                {
                    LogTo.Debug("\{_.username} voted for song \{_.songid}");
                    if (!channels.ContainsKey(_.channel))
                    {
                        LogTo.Warn("Got a vote from \{_.username} for nonexistent channel: \{_.channel}");
                        return;
                    }

                    var playlist = channels[_.channel]._playlist; // TODO Nasty

                    if (playlist.AddVote(_.songid, _.username))
                    {
                        Publish("/\{_.channel}/playlist/\{_.songid}/votes", playlist.GetVotes(_.songid).ToString(), true);
                        Publish("/\{_.channel}/playlist/\{_.songid}/votetime", Timestamp().ToString(), true);
                    }
                }
                else if (_.Message == "unvote")
                {
                    LogTo.Warn("Unvoting currently disabled");
                    return;

                    //Log("\{_.username} unvoted for song \{_.songid}");

                    //// TODO if songid is valid
                    //if (uservotes[_.username + _.songid])
                    //{
                    //    votes[_.songid]--;
                    //    uservotes[_.username + _.songid] = false;

                    //    Publish("/\{_.channel}/playlist/\{_.songid}/votes", votes[_.songid].ToString());
                    //}
                    //else
                    //{
                    //    Log("Unvote was invalid");
                    //    return;
                    //}
                }
                else if (_.Message == "report")
                {
                    LogTo.Debug("\{_.username} reported song \{_.songid} on \{_.channel}");

                    // TODO Record report
                }
                else if (_.Message == "remove")
                {
                    LogTo.Debug("\{_.username} removed song \{_.songid} on \{_.channel}");

                    // TODO Verify priveleges
                    // TODO Remove song
                }
                else if (_.Message == "block")
                {
                    LogTo.Debug("\{_.username} blocked song \{_.songid} on \{_.channel}");

                    // TODO Verify priveleges
                    // TODO Remove song
                    // TODO Record block
                }

                else
                {
                    LogTo.Warn("Invalid command: \{_.Topic}: \{_.Message}");
                    return;
                }
            };

            // ChannelHandler creation

            On["/user/{username}/register/{channel}/{param}"] = _ =>
            {
                // TODO Verify that channel hasn't already been created
                // TODO Check user permissions
                // TODO Publish _.username as moderater
                // TODO Force channel ID to lowercase
                // TODO Separate channel name from ID

                if (string.IsNullOrWhiteSpace(_.Message))
                {
                    LogTo.Warn("\{_.param} was null or whitespace, rejected");
                    return;
                }

                LogTo.Info("\{_.username} created channel \{_.channel}, setting \{_.param} to \{_.Message}");

                List<string> AllowedParams = new List<string> { "name", "description", "image", "moderators" };
                if (AllowedParams.Contains(_.param))
                    Publish("/\{_.channel}/info/\{_.param}", _.Message);
                else
                    LogTo.Warn("Bad param: \{_.param}");
            };

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

            // ChannelHandler registration
            // TODO: ChannelHandler info should probably be stored as JSON data so it comes as one message

            On["/{channel}/info/name"] = _ => // TODO: This should maybe be chan/info/{PARAM}
            {
                LogTo.Info("\{_.channel} got a name: \{_.Message}");

                if (!channels.ContainsKey(_.channel))
                {
                    ChannelHandler channel = chf.CreateChannelHandler(_.channel);
                    channel.Subscribe();
                    channels[_.channel] = channel;
                    ScheduleNextSong(channel);
                }

                channels[_.channel].Name = _.Message;
            };

            On["/{channel}/info/description"] = _ =>
            {
                LogTo.Debug("\{_.channel} got a description: \{_.Message}");

                if (!channels.ContainsKey(_.channel))
                {
                    ChannelHandler channel = chf.CreateChannelHandler(_.channel);
                    channel.Subscribe();
                    channels[_.channel] = channel;
                }

                channels[_.channel].Description = _.Message;
            };

            // TODO Image
            // TODO Moderators
        }

        public new void Run()
        {
            Spotify.Initialize();
            if (!Spotify.Login(File.ReadAllBytes("spotify_appkey.key"), "SOVND_server", _spot.Username, _spot.Password))
                throw new Exception("Login failure");
            LogTo.Trace("Server logged in");
            while (!Spotify.Ready())
                Thread.Sleep(100);
            LogTo.Trace("Server is ready");

            Connect();
            LogTo.Debug("Sever connected");
        }

        private void RemoveSong(string channel, string songID)
        {
            Publish("/\{channel}/playlist/\{songID}/votes", "", true);
            Publish("/\{channel}/playlist/\{songID}/removed", "true", true);
        }

        private void BlockSong(string channel, string songID)
        {
            Publish("/\{channel}/playlist/\{songID}/votes", "", true);
            Publish("/\{channel}/playlist/\{songID}/removed", "true", true);
            Publish("/\{channel}/playlist/\{songID}/blocked", "true", true);
        }

        private Dictionary<ChannelHandler, bool> runningScheduler = new Dictionary<ChannelHandler, bool>();

        private void ScheduleNextSong(ChannelHandler channelHandler)
        {
            lock (runningScheduler)
            {
                if (runningScheduler.ContainsKey(channelHandler) && runningScheduler[channelHandler])
                {
                    LogTo.Debug("Already running scheduler for {0}", channelHandler.MQTTName);
                    return;
                }
                else
                    runningScheduler[channelHandler] = true;
            }

            var task = Task.Factory.StartNew(() =>
            {
                var channel = channelHandler.MQTTName;
                LogTo.Debug("Starting track scheduler for \{channel}");
                
                while (true)
                {
                    Song song = channels[channel].GetTopSong();
                    
                    if (song == null || song.track == null || song.track.Seconds == 0)
                    {
                        if(song != null)
                            song.track = new Track(song.SongID);
                        if (song == null)
                            LogTo.Info("No song");
                        else
                            LogTo.Debug("Either no track or no track time");
                        Thread.Sleep(1000);
                        continue;
                    }
                    else
                    {
                        if (song.track?.Name != null)
                            LogTo.Debug("Playing \{song.track.Name}");

                        Publish("/\{channel}/nowplaying/songid", "", true);
                        Publish("/\{channel}/nowplaying/songid", song.SongID, true);
                        Publish("/\{channel}/nowplaying/starttime", Timestamp().ToString(), true);

                        var songtime = song.track.Seconds;
                        Thread.Sleep((int) Math.Ceiling(songtime*1000));

                        if (song.track?.Name != null)
                            LogTo.Trace("Finished playing \{song.track.Name}");

                        // Set prev song to 0 votes, 0 vote time
                        channelHandler.ClearVotes(song.SongID);

                        Publish("/\{channel}/playlist/\{song.SongID}/votes", "0", true);
                        Publish("/\{channel}/playlist/\{song.SongID}/votetime", Timestamp().ToString(), true);
                        Publish("/\{channel}/playlist/\{song.SongID}/voters", "", true);
                        continue;
                    }
                }
            });
        }

        private static long Timestamp()
        {
            return DateTime.Now.ToUniversalTime().Ticks;
        }
    }
}