using Charlotte;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SOVND.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            (new Server()).Run();
        }
    }

    public class PlaylistTracker : MqttModule
    {
        public Action<string> Log = _ => Console.WriteLine(_);

        public PlaylistTracker()
            : base("127.0.0.1", 1883, "server", "serverpass")
        {
            
        }
        
    }

    public class Server : MqttModule
    {
        public Action<string> Log = _ => Console.WriteLine(_);

        private Dictionary<string, int> votes = new Dictionary<string, int>();
        private Dictionary<string, bool> uservotes = new Dictionary<string, bool>();
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

                    if (!uservotes.ContainsKey(_.username + _.songid) || !uservotes[_.username + _.songid])
                    {
                        Log("Vote was valid");

                        if (!votes.ContainsKey(_.songid))
                        {
                            votes[_.songid] = 0;
                        }
                        votes[_.songid]++;
                        uservotes[_.username + _.songid] = true;

                        Publish("/\{_.channel}/playlist/\{_.songid}/votes", votes[_.songid].ToString());
                        Publish("/\{_.channel}/playlist/\{_.songid}/votetime", Timestamp().ToString());

                        // TODO publish voters
                    }
                    else
                    {
                        Log("Vote was invalid");
                        return;
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

                if (!channels.ContainsKey(_.channel))
                {
                    channels[_.channel] = new Channel();
                    // channel[].moderator = _.username
                }
                // else
                // TODO Check permissions

                List<string> AllowedParams = new List<string> {"name", "description", "image", "moderators"};

                if(AllowedParams.Contains(_.param))
                    Publish("/\{_.channel}/info/\{_.param}", _.Message, true);
                else
                    Log("Bad param: \{_.param}");
            };




            // TODO: This should probably be torn out and modularized because it's shared between the client and the server

            // Channel registration

            On["/{channel}/info/name"] = _ =>
            {
                Log("\{_.channel} got a name: \{_.Message}");

                if (!channels.ContainsKey(_.channel))
                    channels[_.channel] = new Channel();

                channels[_.channel].Name = _.Message;

                // Kick off channel's queue
                ScheduleNextSong(_.channel);
            };

            On["/{channel}/info/description"] = _ =>
            {
                Log("\{_.channel} got a description: \{_.Message}");

                if (!channels.ContainsKey(_.channel))
                    channels[_.channel] = new Channel();

                channels[_.channel].Description = _.Message;
            };

            // TODO Image
            // TODO Moderators

            // Channel playlists
            On["/{channel}/playlist/{songid}/votes"] = _ =>
            {
                if (!channels.ContainsKey(_.channel))
                {
                    Log("Bad channnel: \{_.channel}");
                    return;
                }

                Log("\{_.channel} got a vote for \{_.songid}");

                Channel chan = channels[_.channel];
                if (!chan.SongsByID.ContainsKey(_.songid))
                    chan.SongsByID[_.songid] = new Song() { SongID = _.songid };
                var song = chan.SongsByID[_.songid];
                song.Votes = int.Parse(_.Message);
            };

            On["/{channel}/playlist/{songid}/votetime"] = _ =>
            {
                if (!channels.ContainsKey(_.channel))
                {
                    Log("Bad channnel: \{_.channel}");
                    return;
                }

                Channel chan = channels[_.channel];
                if (!chan.SongsByID.ContainsKey(_.songid))
                    chan.SongsByID[_.songid] = new Song() { SongID = _.songid };
                var song = chan.SongsByID[_.songid];
                song.Votetime = long.Parse(_.Message);
            };
        }

        public new void Run()
        {
            Connect();
            Log("Running");
        }

        private void ScheduleNextSong(string channel, string prevSongID = null)
        {
            // Set prev song to 0 votes, 0 vote time
            if (prevSongID != null)
            {
                Publish("\{channel}/playlist/\{prevSongID}/votes", "0");
                Publish("\{channel}/playlist/\{prevSongID}/votetime", Timestamp().ToString());
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
