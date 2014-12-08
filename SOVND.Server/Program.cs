using Anotar.NLog;
using Ninject;
using Ninject.Extensions.Factory;
using SOVND.Lib;
using System;
using System.Text;
using SOVND.Lib.Handlers;
using SOVND.Lib.Models;
using SOVND.Server.Settings;
using System.Threading;
using System.Linq;
using SOVND.Server.Handlers;
using System.IO;
using SOVND.Lib.Utils;

namespace SOVND.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                LogTo.Debug("===========================================================");

                IKernel kernel = new StandardKernel();

                // Factories
                kernel.Bind<IChannelHandlerFactory>().ToFactory();
                kernel.Bind<IChatProviderFactory>().ToFactory();

                // Singletons
                kernel.Bind<RedisProvider>().ToSelf().InSingletonScope();

                // Standard lifetime
                kernel.Bind<IPlaylistProvider>().To<SortedPlaylistProvider>();
                kernel.Bind<IMQTTSettings>().To<ServerMqttSettings>();

                var server = kernel.Get<Server>();
                server.Run();

                var heartbeat = TimeSpan.FromMinutes(3);
                while (true)
                {
                    File.WriteAllText("sovndserver.heartbeat", Time.Timestamp().ToString());
                    Thread.Sleep(heartbeat);
                }
            }
            catch (Exception e)
            {
                LogTo.FatalException("Unhandled exception", e);
                LogTo.Fatal("Exception stacktrace: {0}", e.StackTrace);
                Thread.Sleep(5000);
                throw;
            }
        }
    }
}
