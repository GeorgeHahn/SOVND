using Charlotte;
using SOVND.Lib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SpotifyClient;

namespace SOVND.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            (new Server()).Run();
        }
    }

    public class Server : MqttModule
    {
        public Action<string> Log = _ => Console.WriteLine(_);
        private Dictionary<string, Channel> channels = new Dictionary<string, Channel>();

        public Server()
            : base("127.0.0.1", 1883, "", "")
        {
            Log("Starting up");

            On["/user/{username}/{channel}/songs/{songid}"] = _ =>
            {
                if (_.Message == "vote")
                {
                    Log("\{_.username} voted for song \{_.songid}");
                    var playlist = channels[_.channel].Playlist;

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

            On["/user/{username}/{channel}/chat"] = _ =>
            {
                Log("\{_.username} said \{_.Message} on \{_.channel}");

                // TODO [LOW] Log chats
                // TODO [LOW] Pass chats from this topic to a channel chat topic
                // TODO [LOW] Allow moderators to mute users
            };

            // Channel creation

            On["/user/{username}/register/{channel}/{param}"] = _ =>
            {
                Log("\{_.username} created channel \{_.channel}, setting \{_.param} to \{_.Message}");

                // TODO Check permissions
                // TODO Publish _.username as moderater

                List<string> AllowedParams = new List<string> {"name", "description", "image", "moderators"};

                if(AllowedParams.Contains(_.param))
                    Publish("/\{_.channel}/info/\{_.param}", _.Message, true);
                else
                    Log("Bad param: \{_.param}");
            };


            // Channel registration
            // TODO: Channel info should probably be stored as JSON data so it comes as one message

            On["/{channel}/info/name"] = _ => // TODO: This should maybe be chan/info/{PARAM}
            {
                Log("\{_.channel} got a name: \{_.Message}");

                if (!channels.ContainsKey(_.channel))
                    channels[_.channel] = new Channel(_.channel);

                channels[_.channel].Name = _.Message;

                // Start watching channel's playlist
                channels[_.channel].Subscribe();

                // Kick off channel's queue
                ScheduleNextSong(_.channel);
            };

            On["/{channel}/info/description"] = _ =>
            {
                Log("\{_.channel} got a description: \{_.Message}");

                if (!channels.ContainsKey(_.channel))
                    channels[_.channel] = new Channel(_.channel);

                channels[_.channel].Description = _.Message;
            };

            // TODO Image
            // TODO Moderators
        }

        public new void Run()
        {
            Spotify.Initialize();
            if (!Spotify.Login(File.ReadAllBytes("spotify_appkey.key"), File.ReadAllText("username.key"), File.ReadAllText("password.key")))
                throw new Exception("Login failure");
            while (!Spotify.Ready())
                Thread.Sleep(100);

            Connect();
            Log("Running");
        }

        private void RemoveSong(string channel, string songID)
        {
            Publish("\{channel}/playlist/\{songID}/votes", "", true);
            Publish("\{channel}/playlist/\{songID}/removed", "true", true);
        }

        private void BlockSong(string channel, string songID)
        {
            Publish("\{channel}/playlist/\{songID}/votes", "", true);
            Publish("\{channel}/playlist/\{songID}/removed", "true", true);
            Publish("\{channel}/playlist/\{songID}/blocked", "true", true);
        }

        private void ScheduleNextSong(string channel, string prevSongID = null)
        {
            // Set prev song to 0 votes, 0 vote time
            if (prevSongID != null)
            {
                Publish("\{channel}/playlist/\{prevSongID}/votes", "0", true);
                Publish("\{channel}/playlist/\{prevSongID}/votetime", Timestamp().ToString(), true);
                Publish("\{channel}/playlist/\{prevSongID}/voters", "");
            }

            var song = channels[channel].GetTopSong()?.SongID;
            if (song != null)
            {
                Publish("\{channel}/nowplaying/songid", song);
                Publish("\{channel}/nowplaying/starttime", Timestamp().ToString());
            }
            else
                Log("No songs in channel \{channel}");

            var task = new Task(() =>
            {
                Thread.Sleep(500); // TODO Song duration or ~500ms if no song
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
