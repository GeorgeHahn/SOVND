using Charlotte;
using System;
using System.Collections.Generic;

namespace SOVND.Lib
{
    public class PlaylistProvider : MqttModule
    {
        public Action<string> Log = _ => Console.WriteLine(_);

        private Dictionary<string, int> votes = new Dictionary<string, int>();
        private Dictionary<string, bool> uservotes = new Dictionary<string, bool>();


        public bool AddVote(string songID, string username)
        {
            if (!uservotes.ContainsKey(username + songID) || !uservotes[username + songID])
            {
                Log("Vote was valid");

                if (!votes.ContainsKey(songID))
                {
                    votes[songID] = 0;
                }
                votes[songID]++;
                uservotes[username + songID] = true;

                // TODO publish voters
                return true;
            }
            else
            {
                Log("Vote was invalid");
                return false;
            }
        }

        public int GetVotes(string songID)
        {
            return votes[songID];
        }

        public PlaylistProvider(Channel channel)
            : base("127.0.0.1", 1883, "", "")
        {
            // Channel playlists
            On["/\{channel.MQTTName}/playlist/{songid}/votes"] = _ =>
            {
                Log("\{channel.Name} got a vote for \{_.songid}");

                if (!channel.SongsByID.ContainsKey(_.songid))
                    channel.SongsByID[_.songid] = new Song() { SongID = _.songid };
                var song = channel.SongsByID[_.songid];
                song.Votes = int.Parse(_.Message);
            };

            On["/\{channel.MQTTName}/playlist/{songid}/votetime"] = _ =>
            {
                if (!channel.SongsByID.ContainsKey(_.songid))
                    channel.SongsByID[_.songid] = new Song() { SongID = _.songid };
                var song = channel.SongsByID[_.songid];
                song.Votetime = long.Parse(_.Message);
            };
        }
    }
}
