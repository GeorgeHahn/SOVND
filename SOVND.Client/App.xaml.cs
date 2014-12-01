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
using SOVND.Client.Settings;
using SOVND.Client.ViewModels;
using SOVND.Lib.Settings;
using Anotar.NLog;
using SOVND.Client.Audio;
using SOVND.Client.Util;
using SOVND.Lib.Handlers;
using SOVND.Lib.Models;
using Newtonsoft.Json;

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
        private readonly IChannelHandlerFactory _chf;

        private readonly string Username;

        public ChannelHandler SubscribedChannelHandler;
        public IntPtr WindowHandle = IntPtr.Zero;

        private Dictionary<string, int> votes = new Dictionary<string, int>();
        private Dictionary<string, bool> uservotes = new Dictionary<string, bool>();
        public Dictionary<string, Channel> channels = new Dictionary<string, Channel>();

        private SpotifyTrackDataPipe streamingaudio = null;
        private Track playingTrack = null;
        private WaveOut player = null;

        private SettingsModel _authSettings;
        private CancellationTokenSource songToken = null;

        private BufferedWaveProvider wave;
        private WaveFormat WaveFormat;

        private object soundlock = new object();

        public SovndClient(IMQTTSettings connectionSettings, IChannelHandlerFactory chf, ISettingsProvider settings)
            : base(connectionSettings.Broker, connectionSettings.Port, settings.GetAuthSettings().SOVNDUsername, settings.GetAuthSettings().SOVNDPassword)
        {
            _chf = chf;
            _authSettings = settings.GetAuthSettings();

            Username = _authSettings.SOVNDUsername;
            Logging.SetupLogging(Username);

            // TODO Track channel list
            // TODO Track playlist for channel

            // On /channel/info -> track channel list
            // On /selectedchannel/ nowplaying,playlist,stats,chat -> track playlist, subscribed channel details

            // TODO: Need to move all of this to somewhere channel specific
            
            // TODO Use channel/nowplaying/starttime to seek to correct position
            // TODO Convert nowplaying to a JSON object so songid and playtime come in at the same time?
            On["/{channel}/nowplaying/songid"] = _ =>
            {
                LogTo.Debug("Playing: \{_.Message}");

                if (string.IsNullOrWhiteSpace(_.Message))
                {
                    playingTrack = null;
                    return;
                }

                // TODO This is plain wrong. Need to hold a ref to the waveout and properly destroy and recreate it when new songs play
                if (playingTrack?.SongID == _.Message)
                    return;

                if (WaveFormat == null)
                {
                    WaveFormat = new WaveFormat(44100, 16, 2);
                }

                if (songToken != null)
                    songToken.Cancel();

                songToken = new CancellationTokenSource();
                var token = songToken.Token;

                Task.Factory.StartNew(() =>
                {
                    lock (soundlock)
                    {
                        using (var player = new WaveOut(WindowHandle))
                        {
                            wave = new BufferedWaveProvider(WaveFormat);
                            wave.BufferDuration = TimeSpan.FromSeconds(15);

                            player.DeviceNumber = 0;
                            player.Init(wave);

                            playingTrack = new Track(_.Message);
                            streamingaudio = new SpotifyTrackDataPipe(playingTrack.TrackPtr, wave);

                            player.Play();

                            while (!streamingaudio.Complete && !token.IsCancellationRequested)
                            {
                                Thread.Sleep(15);
                            }

                            streamingaudio.StopStreaming();
                            player.Pause();
                        }
                    }
                }, token);
            };


            // TODO: We don't need to be subbed to this all the time, just when browsing for channels
            On["/{channel}/info"] = _ =>
            {
                Channel channel = JsonConvert.DeserializeObject<Channel>(_.Message);

                if (!channels.ContainsKey(_.channel))
                    channels[_.channel] = channel;
            };

            SubscribedChannelHandler = chf.CreateChannelHandler("ambient");
        }

        internal void SendChat(string text)
        {
            if (SubscribedChannelHandler != null)
                Publish("/user/\{Username}/\{SubscribedChannelHandler.MQTTName}/chat", text);
            else
                LogTo.Warn("Cannot send chat: not subscribed to a channel");
        }

        public bool RegisterChannel(string name, string description, string image)
        {
            var channel = new Channel
            {
                Name = name,
                Description = description
            };
            return RegisterChannel(channel);
        }

        public bool RegisterChannel(Channel channel)
        {
            // TODO: Detect success or figure out a way to come close (eg check channels that have been registered locally)

            if (channel == null || string.IsNullOrWhiteSpace(channel.Name))
                return false;

            var msg = JsonConvert.SerializeObject(channel);

            Publish("/user/\{Username}/register/\{channel.Name}", msg);
            return true;
        }

        internal void SubscribeToChannel(string channel)
        {
            this.SubscribedChannelHandler = _chf.CreateChannelHandler(channel);
        }

        public void AddTrack(Track track)
        {
            if (SubscribedChannelHandler != null && SubscribedChannelHandler.MQTTName != null)
            {
                Publish("/user/\{Username}/\{SubscribedChannelHandler.MQTTName}/songs/\{track.SongID}", "vote");
                Publish("/user/\{Username}/\{SubscribedChannelHandler.MQTTName}/songssearch/", track.Name + " " + track?.Artists[0]);
            }
            else
                LogTo.Warn("Not subscribed to a channel or channel subscription is malformed (null or MQTTName null)");
        }

        protected override void Stop()
        {
            if(streamingaudio != null)
                streamingaudio.StopStreaming();
        }
    }
}
