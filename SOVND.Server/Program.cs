using Anotar.NLog;
using Ninject;
using Ninject.Extensions.Factory;
using SOVND.Lib;
using System;
using System.Text;
using SOVND.Lib.Handlers;
using SOVND.Lib.Models;

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
                kernel.Bind<IServer>().To<Server>();
                kernel.Bind<IPlaylistProvider>().To<PlaylistProvider>();
                kernel.Bind<IChatProvider>().To<ChatProvider>();

                kernel.Bind<IMQTTSettings>().To<ServerMqttSettings>();

                var server = kernel.Get<IServer>();
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
