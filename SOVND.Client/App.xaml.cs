using Charlotte;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using SpotifyClient;

namespace SOVND.Client
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public SovndClient client = new SovndClient("127.0.0.1", 1883, "", "");
    }

    public class SovndClient : MqttModule
    {
        private Action<string> Log = _ => Console.WriteLine(_);

        public SovndClient(string brokerHostName, int brokerPort, string username, string password)
            : base(brokerHostName, brokerPort, username, password)
        {
            // TODO Track channel list
            // TODO Track playlist for channel

            // On /channel/info -> track channel list
            // On /selectedchannel/ nowplaying,playlist,stats,chat -> track playlist, subscribed channel details


            On["/{channel}/playlist/{songid}"] = _ =>
            {
                Log("Votes for :\{_.songid} set to :\{_.Message}");
            };

            On["/{channel}/playlist/{songid}/voters"] = _ =>
            {
                Log("Voters for :\{_.songid}: :\{_.Message}");
            };

            On["/{channel}/playlist/{songid}/removed"] = _ =>
            {
                Log("Song removed from playlist: :\{_.songid}");
            };

            On["/user/{username}/{channel}/chat"] = _ =>
            {
                Log(":\{_.username}: :\{_.Message}");
            };

            On["/{channel}/stats/users"] = _ =>
            {
                Log(":\{_.Message} active users");
            };

            On["/{channel}/nowplaying"] = _ =>
            {
                Log("Playing: :\{_.Message}");
            };
        }

        public new void Run()
        {
            Connect();

            RegisterChannel("Ambient", "Ambient music", "");

            Publish("/user/georgehahn/ambient/songs/spotify:track:4OeTOlMKA693c7YOh5z9x1", "vote");
        }

        public bool RegisterChannel(string name, string description, string image)
        {
            Publish("/user/georgehahn/registerchannel/name", name);
            Publish("/user/georgehahn/registerchannel/description", description);
            Publish("/user/georgehahn/registerchannel/image", image);

            return true;
        }
    }

    public class SpotifySong
    {
        
    }
}
