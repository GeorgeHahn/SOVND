﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ninject;
using Ninject.Extensions.Factory;
using SOVND.Client.Settings;
using SOVND.Lib.Handlers;
using SOVND.Lib.Models;
using SOVND.Lib.Settings;
using SOVND.Client.Modules;
using SOVND.Client.Util;

namespace SOVND.Client
{
    static class Program
    {
        [STAThread]
        public static void Main()
        {
            IKernel kernel = new StandardKernel();
            kernel.Bind<IMQTTSettings>().To<SovndMqttSettings>();
            kernel.Bind<IPlaylistProvider>().To<ObservablePlaylistProvider>();
            kernel.Bind<ISettingsProvider>().To<FilesystemSettingsProvider>();
            kernel.Bind<IFileLocationProvider>().To<AppDataLocationProvider>();
            kernel.Bind<IAppName>().To<AppName>();

            // TODO Refactor this out
            kernel.Bind<SyncHolder>().ToSelf().InSingletonScope();

            // Singleton classes
            kernel.Bind<ChannelDirectory>().ToSelf().InSingletonScope();
            kernel.Bind<SovndClient>().ToSelf().InSingletonScope();

            // Factories
            kernel.Bind<IChannelHandlerFactory>().ToFactory();
            kernel.Bind<IChatProviderFactory>().ToFactory();
            kernel.Bind<IPlayerFactory>().ToFactory();

            // Instantiating this class checks settings and shows UI if they're not set
            kernel.Get<CheckSettings>();

            // Instantiating this initializes Spotify
            kernel.Get<StartSpotify>();

            var window = kernel.Get<MainWindow>();

            var app = kernel.Get<App>();
            app.Run(window);
        }
    }
}
