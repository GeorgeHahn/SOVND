using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOVND.Client.Modules
{
    public interface IPlayerFactory
    {
        NowPlayingHandler CreatePlayer(string channelName);
    }
}
