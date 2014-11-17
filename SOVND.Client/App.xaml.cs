using Charlotte;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

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

            Connect();

            Publish("/user/georgehahn/ambient/songs/spotify:track:4OeTOlMKA693c7YOh5z9x1", "vote");
        }
    }
}
