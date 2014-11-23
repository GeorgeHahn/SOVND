using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Charlotte;
using SOVND.Lib;
using SpotifyClient;

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

            On["/user/{username}/{channel}/songs/{songid}"] = _ =>
            {
                if (_.Message == "vote")
                {
                    Log("\{_.username} voted for song \{_.songid}");
                    var playlist = channels[_.channel]._playlist; // TODO Nasty

                    if (playlist.AddVote(_.songid, _.username))
                    {
                        Publish("/\{_.channel}/playlist/\{_.songid}/votes", playlist.GetVotes(_.songid).ToString(), true);
                        Publish("/\{_.channel}/playlist/\{_.songid}/votetime", Timestamp().ToString(), true);
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
                Log("\{_.username} created channel \{_.channel}, setting \{_.param} to \{_.Message}");

                // TODO Check permissions
                // TODO Publish _.username as moderater
                // TODO If channel hasn't already been created

                List<string> AllowedParams = new List<string> {"name", "description", "image", "moderators"};

                if(AllowedParams.Contains(_.param))
                    Publish("/\{_.channel}/info/\{_.param}", _.Message, true);
                else
                    Log("Bad param: \{_.param}");
            };

            On["/user/{username}/{channel}/chat"] = _ =>
            {
                Log("\{_.channel}-> \{_.username}: \{_.Message}");
                
                // TODO [LOW] Log chats
                // TODO [LOW] Allow moderators to mute users
                
                Publish("/\{_.channel}/chat", "\{_.username}: \{_.Message}");
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
                    ScheduleNextSong(_.channel);
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
            while (!Spotify.Ready())
                Thread.Sleep(100);

            Connect();
            Log("Running");
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

        private void ScheduleNextSong(string channel, Song prevSong = null)
        {
            // Set prev song to 0 votes, 0 vote time
            if (prevSong != null)
            {
                if(prevSong.track?.Name != null)
                    Log("Finished playing \{prevSong.track.Name}");

                Publish("/\{channel}/playlist/\{prevSong.SongID}/votes", "0", true);
                Publish("/\{channel}/playlist/\{prevSong.SongID}/votetime", Timestamp().ToString(), true);
                Publish("/\{channel}/playlist/\{prevSong.SongID}/voters", "");
            }

            var song = channels[channel].GetTopSong();
            if (song != null)
            {
                if(song.track?.Name != null)
                    Log("Playing \{song.track.Name}");
                Publish("/\{channel}/nowplaying/songid", song.SongID, true);
                Publish("/\{channel}/nowplaying/starttime", Timestamp().ToString(), true);
            }
            else
                Log("No songs in channel \{channel}");

            var task = new Task(() =>
            {
                if (song == null || song.track == null || song.track.Seconds == 0)
                    Thread.Sleep(500);
                else
                    Thread.Sleep((int)Math.Ceiling(song.track.Seconds*1000));

                ScheduleNextSong(channel, song);
            });
            task.Start();
        }

        private static long Timestamp()
        {
            return DateTime.Now.ToUniversalTime().Ticks;
        }
    }
}