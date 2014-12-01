using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.Collections;
using SOVND.Lib;
using System.Threading;
using Ninject;
using Ninject.Extensions.Factory;
using SOVND.Client.Settings;
using SOVND.Client.ViewModels;
using SOVND.Lib.Settings;
using SOVND.Lib.Handlers;
using SOVND.Lib.Models;

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
}
