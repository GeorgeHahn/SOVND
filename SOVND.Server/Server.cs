using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Charlotte;
using SOVND.Lib;
using SpotifyClient;
using System.Diagnostics;

namespace SOVND.Server
{
    public interface IServer
    {
        void Run();
        void Disconnect();
    }

    public class ServerSpotifyAuth
    {
        public string Username
        {
            get { return File.ReadAllText("spot.username.key"); }
        }

        public string Password
        {
            get { return File.ReadAllText("spot.password.key"); }
        }
    }

    public class Server : MqttModule, IServer
    {
        private readonly ServerSpotifyAuth _spot;
        public Action<string> Log = _ => Console.WriteLine(_);
        private Dictionary<string, ChannelHandler> channels = new Dictionary<string, ChannelHandler>();

        public Server(IMQTTSettings settings, IChannelHandlerFactory chf, ServerSpotifyAuth spot)
            : base(settings.Broker, settings.Port, settings.Username, settings.Password)
        {
            _spot = spot;
            Log("Starting up");

            On["#"] = _ =>
            {
                Log("\{_.Topic}: \{_.Message}");
            };


            On["/user/{username}/{channel}/songssearch/"] = _ =>
            {
                Search search = Spotify.GetSearch(_.Message);
                Log("Searched \{_.Message} and is is \{(search.IsLoaded?"is":"is not")} loaded");
            };

            On["/user/{username}/{channel}/songs/{songid}"] = _ =>
            {
                if (_.Message == "vote")
                {
                    Log("\{_.username} voted for song \{_.songid}");
                    if (!channels.ContainsKey(_.channel))
                    {
                        Log("Got a vote from \{_.username} for nonexistent channel: \{_.channel}");
                        return;
                    }

                    var playlist = channels[_.channel]._playlist; // TODO Nasty

                    if (playlist.AddVote(_.songid, _.username))
                    {
                        Publish("/\{_.channel}/playlist/\{_.songid}/votes", playlist.GetVotes(_.songid).ToString());
                        Publish("/\{_.channel}/playlist/\{_.songid}/votetime", Timestamp().ToString());
                    }
                }
                else if (_.Message == "unvote")
                {
                    Log("Unvoting currently disabled");
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
                    Log("\{_.username} reported song \{_.songid} on \{_.channel}");

                    // TODO Record report
                }
                else if (_.Message == "remove")
                {
                    Log("\{_.username} removed song \{_.songid} on \{_.channel}");

                    // TODO Verify priveleges
                    // TODO Remove song
                }
                else if (_.Message == "block")
                {
                    Log("\{_.username} blocked song \{_.songid} on \{_.channel}");

                    // TODO Verify priveleges
                    // TODO Remove song
                    // TODO Record block
                }

                else
                {
                    Log("Invalid command: \{_.Topic}: \{_.Message}");
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
                    Log("\{_.param} was null or whitespace, rejected");
                    return;
                }

                Log("\{_.username} created channel \{_.channel}, setting \{_.param} to \{_.Message}");

                List<string> AllowedParams = new List<string> { "name", "description", "image", "moderators" };
                if (AllowedParams.Contains(_.param))
                    Publish("/\{_.channel}/info/\{_.param}", _.Message);
                else
                    Log("Bad param: \{_.param}");
            };

            On["/user/{username}/{channel}/chat"] = _ =>
            {
                Log("\{_.channel}-> \{_.username}: \{_.Message}");
                
                // TODO [LOW] Log chats
                // TODO [LOW] Allow moderators to mute users

                if (channels.ContainsKey(_.channel))
                    Publish("/\{_.channel}/chat", "\{_.username}: \{_.Message}");
                else
                    Log("Chat was for invalid channel");
            };

            // ChannelHandler registration
            // TODO: ChannelHandler info should probably be stored as JSON data so it comes as one message

            On["/{channel}/info/name"] = _ => // TODO: This should maybe be chan/info/{PARAM}
            {
                Log("\{_.channel} got a name: \{_.Message}");

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
                Log("\{_.channel} got a description: \{_.Message}");

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
            Log("Logged in");
            while (!Spotify.Ready())
                Thread.Sleep(100);
            Log("Ready");

            Connect();
            Log("Connected");
        }

        private void RemoveSong(string channel, string songID)
        {
            Publish("/\{channel}/playlist/\{songID}/votes", "");
            Publish("/\{channel}/playlist/\{songID}/removed", "true");
        }

        private void BlockSong(string channel, string songID)
        {
            Publish("/\{channel}/playlist/\{songID}/votes", "");
            Publish("/\{channel}/playlist/\{songID}/removed", "true");
            Publish("/\{channel}/playlist/\{songID}/blocked", "true");
        }

        private Dictionary<ChannelHandler, bool> runningScheduler = new Dictionary<ChannelHandler, bool>();

        private void ScheduleNextSong(ChannelHandler channelHandler)
        {
            lock (runningScheduler)
            {
                if (runningScheduler.ContainsKey(channelHandler) && runningScheduler[channelHandler])
                {
                    Log("Already running scheduler for \{channelHandler.MQTTName}");
                    return;
                }
                else
                    runningScheduler[channelHandler] = true;
            }

            var task = Task.Factory.StartNew(() =>
            {
                var channel = channelHandler.MQTTName;
                Log("Starting track scheduler for \{channel}");
                
                while (true)
                {
                    var song = channels[channel].GetTopSong();
                    
                    if (song == null || song.track == null || song.track.Seconds == 0)
                    {
                        if(song != null)
                            song.track = new Track(song.SongID);
                        Log("Either no song, no track, or no track time; sleeping 1s");
                        Thread.Sleep(1000);
                        continue;
                    }
                    else
                    {
                        if (song.track?.Name != null)
                            Log("Playing \{song.track.Name}");

                        Publish("/\{channel}/nowplaying/songid", "");
                        Publish("/\{channel}/nowplaying/songid", song.SongID);
                        Publish("/\{channel}/nowplaying/starttime", Timestamp().ToString());

                        var songtime = song.track.Seconds;
                        Thread.Sleep((int) Math.Ceiling(songtime*1000));

                        if (song.track?.Name != null)
                            Log("Finished playing \{song.track.Name}");

                        // Set prev song to 0 votes, 0 vote time
                        channelHandler._playlist.ClearVotes(song.SongID);

                        Publish("/\{channel}/playlist/\{song.SongID}/votes", "0");
                        Publish("/\{channel}/playlist/\{song.SongID}/votetime", Timestamp().ToString());
                        Publish("/\{channel}/playlist/\{song.SongID}/voters", "");
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