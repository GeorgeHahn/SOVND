using System;
using System.IO;
using System.Threading;
using Anotar.NLog;
using HipchatApiV2.Enums;
using Ninject;
using Ninject.Extensions.Factory;
using SOVND.Lib.Handlers;
using SOVND.Lib.Models;
using SOVND.Lib.Utils;
using SOVND.Server.Handlers;
using SOVND.Server.Settings;
using SOVND.Server.Utils;

namespace SOVND.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
            {
                var exception = eventArgs.ExceptionObject as Exception;

                string message = string.Format("Unhandled exception: {0}: {1} \r\n{2}", exception.GetType().ToString(),
                    exception.Message, exception.StackTrace);

                LogTo.Fatal(message);
                HipchatSender.SendNotification("Server errors", message, RoomColors.Random);
            };

            try
            {
                LogTo.Debug("======================= STARTING ==========================");

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
            finally
            {
                LogTo.Debug("========================= DEAD ============================");
            }
        }
    }
}
