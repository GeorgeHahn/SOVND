using System;
using System.IO;
using System.Threading;
using System.Windows;
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
    internal static class Program
    {
        public static IKernel kernel;

        [STAThread]
        public static void Main()
        {
            if (AlreadyRunning())
                return;

            kernel = new StandardKernel();
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

            LogTo.Trace("Instantiating app XAML");
            var app = kernel.Get<App>();

            LogTo.Trace("Instantiating main window");
            var window = kernel.Get<MainWindow>();

            LogTo.Trace("Running");
            app.Run(window);
        }

        private static bool AlreadyRunning()
        {
            bool exclusive;
            using (Mutex m = new Mutex(true, "b1cddd64-d226-40ca-9a17-1221174b3e2c", out exclusive))
            {
                if (exclusive)
                {
                    return false;
                }
                MessageBox.Show("SOVND is already running.", "SOVND is already running", MessageBoxButton.OK, MessageBoxImage.Information);
                return true;
            }
        }
    }

    public class ViewModelLocator
    {
        public MainWindowViewModel MainWindowViewModel
        {
            get { return Program.kernel.Get<MainWindowViewModel>(); }
        }
    }
}
