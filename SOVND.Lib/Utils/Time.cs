using System;

namespace SOVND.Lib.Utils
{
    public class Time
    {
        public static long Timestamp()
        {
            return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds;
        }
    }
}