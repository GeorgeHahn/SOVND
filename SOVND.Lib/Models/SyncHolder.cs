using System.Threading;

namespace SOVND.Lib.Models
{
    // TODO This is super super super bad
    public static class SyncHolder
    {
        public static SynchronizationContext sync;
    }
}