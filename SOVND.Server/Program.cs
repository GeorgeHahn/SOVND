using Anotar.NLog;
using Ninject;
using Ninject.Extensions.Factory;
using SOVND.Lib;
using System;
using System.Text;
using SOVND.Lib.Handlers;
using SOVND.Lib.Models;
using SOVND.Server.Settings;

namespace SOVND.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                IKernel kernel = new StandardKernel();
                kernel.Bind<IChannelHandlerFactory>().ToFactory();
                kernel.Bind<IPlaylistProvider>().To<PlaylistProvider>();
                kernel.Bind<IChatProvider>().To<ChatProvider>();

                kernel.Bind<IMQTTSettings>().To<ServerMqttSettings>();

                var server = kernel.Get<Server>();
                server.Run();
            }
            catch (Exception e)
            {
                LogTo.FatalException("Unhandled exception", e);
                throw;
            }
        }
    }
}
