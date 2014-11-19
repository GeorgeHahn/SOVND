using Charlotte;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using SpotifyClient;
using SOVND.Server;
using System.Collections;

namespace SOVND.Client
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static SovndClient client = new SovndClient("127.0.0.1", 1883, "", "");
    }

    public class SovndClient : MqttModule
    {
        private Action<string> Log = _ => Console.WriteLine(_);

        public string Username { get; private set; } = "georgehahn";

        public List<Track> Playlist { get; } = new List<Track>();

        //public IEnumerable<Track> Playlist
        //{
        //    get { yield return channels["ambient"].GetTopSong()?.track; } // TODO Give all songs in channel
        //}

        private Dictionary<string, int> votes = new Dictionary<string, int>();
        private Dictionary<string, bool> uservotes = new Dictionary<string, bool>();
        public Dictionary<string, Channel> channels = new Dictionary<string, Channel>();

        public SovndClient(string brokerHostName, int brokerPort, string username, string password)
            : base(brokerHostName, brokerPort, username, password)
        {
            // TODO Track channel list
            // TODO Track playlist for channel

            // On /channel/info -> track channel list
            // On /selectedchannel/ nowplaying,playlist,stats,chat -> track playlist, subscribed channel details


            On["/{channel}/playlist/{songid}"] = _ =>
            {
                Log("Votes for \{_.songid} set to \{_.Message}");
            };

            On["/{channel}/playlist/{songid}/voters"] = _ =>
            {
                Log("Voters for \{_.songid}: \{_.Message}");
            };

            On["/{channel}/playlist/{songid}/removed"] = _ =>
            {
                Log("Song removed from playlist: \{_.songid}");
            };

            On["/user/{username}/{channel}/chat"] = _ =>
            {
                Log("\{_.username}: \{_.Message}");
            };

            On["/{channel}/stats/users"] = _ =>
            {
                Log("\{_.Message} active users");
            };

            On["/{channel}/nowplaying"] = _ =>
            {
                Log("Playing: \{_.Message}");
            };



            On["/{channel}/info/name"] = _ =>
            {
                if (!channels.ContainsKey(_.channel))
                    channels[_.channel] = new Channel();

                channels[_.channel].Name = _.Message;
            };

            On["/{channel}/info/description"] = _ =>
            {
                if (!channels.ContainsKey(_.channel))
                    channels[_.channel] = new Channel();

                channels[_.channel].Description = _.Message;
            };

            // Channel playlists
            On["/{channel}/playlist/{songid}/votes"] = _ =>
            {
                if (!channels.ContainsKey(_.channel))
                {
                    Log("Bad channnel: \{_.channel}");
                    return;
                }

                Track track = null;

                while (track != null)
                {
                    // TODO Max retry count
                    try
                    {
                        track = new Track(_.songid);
                    }
                    catch (InvalidOperationException)
                    {
                        Log("Track not loaded");
                    }
                }

                Channel chan = channels[_.channel];
                if (!chan.SongsByID.ContainsKey(_.songid))
                    chan.SongsByID[_.songid] = new Song()
                    {
                        SongID = _.songid,
                        track = track
                    };
                Playlist.Add(track);
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

        public bool RegisterChannel(string name, string description, string image)
        {
            Publish("/user/\{Username}/register/\{name}/name", name);
            Publish("/user/\{Username}/register/\{name}/description", description);
            Publish("/user/\{Username}/register/\{name}/image", image);

            return true;
        }

        public void AddTrack(Track track)
        {
            Publish("/user/\{Username}/ambient/songs/\{Spotify.GetTrackLink(track.TrackPtr)}", "vote");
        }
    }
}
