using System.Windows;
using System.Threading;
using Ninject;
using Ninject.Extensions.Factory;
using SOVND.Client.Modules;
using SOVND.Client.Settings;
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

        public App(SovndClient client, MainWindow ui)
        {
            App.client = client;
            ui.Show();
        }
    }
}
