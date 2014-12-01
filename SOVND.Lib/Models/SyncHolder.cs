using System.Threading;

namespace SOVND.Lib.Models
{
    // TODO This is super super super bad
    public class SyncHolder
    {
        public SynchronizationContext sync { get; set; }
    }
}