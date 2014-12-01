using System.Collections.ObjectModel;
using System.Linq;
using SOVND.Lib.Models;

namespace SOVND.Client.Util
{
    public class ChannelDirectory
    {
        public ObservableCollection<Channel> channels = new ObservableCollection<Channel>();

        public bool AddChannel(Channel channel)
        {
            if (channels.Where(x => x.Name == channel.Name).Count() > 0)
                return false;

            channels.Add(channel);
            return true;
        }
    }
}