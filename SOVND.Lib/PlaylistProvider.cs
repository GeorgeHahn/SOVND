using Charlotte;
using SpotifyClient;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SOVND.Lib
{
    public class PlaylistProvider : MqttModule
    {
        private readonly Channel _channel;
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


                // TODO publish voter names
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

        public IEnumerable<Song> InOrder() // TODO need to give WPF something IObservable to bind to
        {
            _channel.Songs.Sort();
            return _channel.Songs;
        }

        private void AddNewSong(string ID)
        {
            var song = new Song(ID);
            _channel.SongsByID[ID] = song;
            _channel.Songs.Add(song);

            WaitForTrack(song);
        }

        private void WaitForTrack(Song song)
        {
            (new Task(() =>
            {
                if (song.track == null)
                {
                    Thread.Sleep(100);
                    WaitForTrack(song);
                }
                else
                    Log("Song is \{song.track.Name}");
            })).Start();
        }

        public PlaylistProvider(Channel channel)
            : base("127.0.0.1", 1883, "", "")
        {
            _channel = channel;
            // Channel playlists
            On["/\{channel.MQTTName}/playlist/{songid}/votes"] = _ =>
            {
                Log("\{channel.Name} got a vote for \{_.songid}");

                if (!channel.SongsByID.ContainsKey(_.songid))
                    AddNewSong(_.songid);
                var song = channel.SongsByID[_.songid];
                song.Votes = int.Parse(_.Message);
            };

            On["/\{channel.MQTTName}/playlist/{songid}/votetime"] = _ =>
            {
                if (!channel.SongsByID.ContainsKey(_.songid))
                    AddNewSong(_.songid);
                var song = channel.SongsByID[_.songid];
                song.Votetime = long.Parse(_.Message);
            };

            Run();
        }
    }
}
