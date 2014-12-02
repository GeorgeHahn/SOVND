using System.Collections.ObjectModel;
using System.Linq;
using SOVND.Lib.Models;

namespace SOVND.Client.Util
{
    public class ChannelDirectory
    {
        private readonly SyncHolder _sync;
        public ObservableCollection<Channel> channels = new ObservableCollection<Channel>();

        public ChannelDirectory(SyncHolder sync)
        {
            _sync = sync;
        }

        public bool AddChannel(Channel channel)
        {
            if (channels.Where(x => x.Name == channel.Name).Count() > 0)
                return false;

            if (_sync.sync != null)
                _sync.sync.Send((x) => channels.Add(channel), null);
            else
                channels.Add(channel);
            return true;
        }
    }
}