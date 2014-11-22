using Charlotte;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using SpotifyClient;
using System.Collections;
using SOVND.Lib;
using NAudio.Wave;
using System.Threading;
using Ninject;
using Ninject.Extensions.Factory;
using SOVND.Client.ViewModels;
using SOVND.Lib.Settings;

namespace SOVND.Client
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static SovndClient client;
        public static SynchronizationContext uithread;

        public App()
        {
            IKernel kernel = new StandardKernel();
            kernel.Bind<IMQTTSettings>().To<SovndMqttSettings>();
            kernel.Bind<IChannelHandlerFactory>().ToFactory();
            kernel.Bind<IPlaylistProvider>().To<PlaylistProvider>();
            kernel.Bind<IChatProvider>().To<ChatProvider>();
            kernel.Bind<ISettingsProvider>().To<FilesystemSettingsProvider>();
            kernel.Bind<IFileLocationProvider>().To<AppDataLocationProvider>();

            kernel.Bind<IAppName>().To<AppName>();

            client = kernel.Get<SovndClient>();
            var window = kernel.Get<MainWindow>();
            window.Show();
        }
    }

    public class SovndClient : MqttModule
    {
        private Action<string> Log = _ => Console.WriteLine(_);

        private readonly string Username;

        public ChannelHandler SubscribedChannelHandler;
        public IntPtr WindowHandle = IntPtr.Zero;

        private Dictionary<string, int> votes = new Dictionary<string, int>();
        private Dictionary<string, bool> uservotes = new Dictionary<string, bool>();
        public Dictionary<string, ChannelHandler> channels = new Dictionary<string, ChannelHandler>();

        private SpotifyTrackDataPipe streamingaudio = null;
        private Track playingTrack = null;
        private WaveOut player = null;

        private SettingsModel _authSettings;

        public SovndClient(IMQTTSettings connectionSettings, IChannelHandlerFactory chf, ISettingsProvider settings)
            : base(connectionSettings.Broker, connectionSettings.Port, settings.GetAuthSettings().SOVNDUsername, settings.GetAuthSettings().SOVNDPassword)
        {
            _authSettings = settings.GetAuthSettings();

            Username = _authSettings.SOVNDUsername;

            // TODO Track channel list
            // TODO Track playlist for channel

            // On /channel/info -> track channel list
            // On /selectedchannel/ nowplaying,playlist,stats,chat -> track playlist, subscribed channel details

            // TODO: Need to move all of this to somewhere channel specific
            
            // TODO Use channel/nowplaying/starttime to seek to correct position
            // TODO Convert nowplaying to a JSON object so songid and playtime come in at the same time?
            On["/{channel}/nowplaying/songid"] = _ =>
            {
                Log("Playing: \{_.Message}");

                // TODO This is plain wrong. Need to hold a ref to the waveout and properly destroy and recreate it when new songs play
                if (playingTrack?.SongID == _.Message)
                    return;

                playingTrack = new Track(_.Message);
                streamingaudio = new SpotifyTrackDataPipe(playingTrack.TrackPtr);
                player = new WaveOut(WindowHandle);
                player.DeviceNumber = 0;
                player.Init(streamingaudio.wave);
                player.Play();
            };


            On["/{channel}/info/name"] = _ =>
            {
                if (!channels.ContainsKey(_.channel))
                    channels[_.channel] = chf.CreateChannelHandler(_.channel);

                channels[_.channel].Name = _.Message;
            };

            On["/{channel}/info/description"] = _ =>
            {
                if (!channels.ContainsKey(_.channel))
                    channels[_.channel] = chf.CreateChannelHandler(_.channel);

                channels[_.channel].Description = _.Message;
            };

            //// Channel playlists
            //On["/{channel}/playlist/{songid}/votes"] = _ =>
            //{
            //    if (!channels.ContainsKey(_.channel))
            //    {
            //        Log("Bad channnel: \{_.channel}");
            //        return;
            //    }

            //    Track track = null;

            //    while (track != null)
            //    {
            //        // TODO Max retry count
            //        try
            //        {
            //            track = new Track(_.songid);
            //        }
            //        catch (InvalidOperationException)
            //        {
            //            Log("Track not loaded");
            //        }
            //    }

            //    Channel chan = channels[_.channel];
            //    if (!chan.SongsByID.ContainsKey(_.songid))
            //        chan.SongsByID[_.songid] = new Song()
            //        {
            //            SongID = _.songid,
            //            track = track
            //        };
            //    Playlist.Add(track);
            //    var song = chan.SongsByID[_.songid];
            //    song.Votes = int.Parse(_.Message);
            //};

            //On["/{channel}/playlist/{songid}/votetime"] = _ =>
            //{
            //    if (!channels.ContainsKey(_.channel))
            //    {
            //        Log("Bad channnel: \{_.channel}");
            //        return;
            //    }

            //    Channel chan = channels[_.channel];
            //    if (!chan.SongsByID.ContainsKey(_.songid))
            //        chan.SongsByID[_.songid] = new Song() { SongID = _.songid };
            //    var song = chan.SongsByID[_.songid];
            //    song.Votetime = long.Parse(_.Message);
            //};

            SubscribedChannelHandler = chf.CreateChannelHandler("ambient");
        }

        internal void SendChat(string text)
        {
            Publish("/user/\{Username}/ambient/chat", text);
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
            // TODO ambient -> subscribed channel
            Publish("/user/\{Username}/ambient/songs/\{Spotify.GetTrackLink(track.TrackPtr)}", "vote");
        }

        protected override void Stop()
        {
            streamingaudio.StopStreaming();
        }
    }
}
