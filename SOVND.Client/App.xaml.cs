using System.Windows;
using System.Threading;
using Ninject;
using Ninject.Extensions.Factory;
using SOVND.Client.Modules;
using SOVND.Client.Settings;
using SOVND.Lib.Settings;
using SOVND.Lib.Handlers;
using SOVND.Lib.Models;
using System;

namespace SOVND.Client
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App(SovndClient client, MainWindow UI)
        {
            App.Client = client;
            UI.Show();
        }

        public static IntPtr WindowHandle { get; internal set; }

        public static SovndClient Client { get; internal set; }

        public static SynchronizationContext UIThread { get; internal set; }
    }
}
