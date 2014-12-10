using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using SOVND.Lib.Models;

namespace SOVND.Client.Util
{
    public class ChannelDirectory
    {
        public ObservableCollection<Channel> channels = new ObservableCollection<Channel>();

        public async Task<bool> AddChannel(Channel channel)
        {
            if (channels.Count(x => x.Name == channel.Name) > 0)
                return false;
            
            channels.Add(channel);
            return true;
        }
    }
}