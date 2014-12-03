using System;

namespace SOVND.Lib.Utils
{
    public class Util
    {
        public static long Timestamp()
        {
            return DateTime.Now.ToUniversalTime().Ticks;
        }
    }
}