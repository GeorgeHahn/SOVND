using System;
using System.IO;
using Anotar.NLog;
using Ninject;
using Ninject.Extensions.Factory;
using SOVND.Client.Modules;
using SOVND.Client.Settings;
using SOVND.Client.Util;
using SOVND.Lib.Handlers;
using SOVND.Lib.Models;
using SOVND.Lib.Settings;

namespace SOVND.Client
{
    static class Program
    {
        [STAThread]
        public static void Main()
        {
            try
            {
                LogTo.Trace("Starting client (version TODO)");

                IKernel kernel = new StandardKernel();
                kernel.Bind<IMQTTSettings>().To<SovndMqttSettings>();
                kernel.Bind<IPlaylistProvider>().To<ObservablePlaylistProvider>();
                kernel.Bind<ISettingsProvider>().To<FilesystemSettingsProvider>();
                kernel.Bind<IFileLocationProvider>().To<AppDataLocationProvider>();
                kernel.Bind<IAppName>().To<AppName>();

                // Singleton classes
                kernel.Bind<ChannelDirectory>().ToSelf().InSingletonScope();
                kernel.Bind<SovndClient>().ToSelf().InSingletonScope();

                // Factories
                kernel.Bind<IChannelHandlerFactory>().ToFactory();
                kernel.Bind<IChatProviderFactory>().ToFactory();
                kernel.Bind<IPlayerFactory>().ToFactory();

                LogTo.Trace("All libraries bound, checking settings");

                // Instantiating this class checks settings and shows UI if they're not set
                kernel.Get<CheckSettings>();

                LogTo.Trace("Settings checked, starting libspotify");

                // Instantiating this initializes Spotify
                kernel.Get<StartSpotify>();

                LogTo.Trace("Instantiating main window");
                var window = kernel.Get<MainWindow>();

                LogTo.Trace("Running");
                var app = kernel.Get<App>();
                app.Run(window);
            }
            catch (Exception e)
            {
                File.WriteAllText("ERROR.log", e.GetType() + Environment.NewLine + e.Message + Environment.NewLine + e.StackTrace);
                throw;
            }
        }
    }
}
