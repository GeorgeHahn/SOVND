using System;
using System.Collections;
using SOVND.Lib.Models;

namespace SOVND.Lib.Utils
{
    public class SongComparer : IComparer
    {
        public int Compare(object x, object y)
        {
            var xs = x as Song;
            var ys = y as Song;
            if (xs == null)
                throw new ArgumentOutOfRangeException("x");
            if(ys == null)
                throw new ArgumentOutOfRangeException("y");

            if (xs.Playing && ys.Playing)
                return xs.CompareTo(y);

            if (xs.Playing)
                return -1;
            if (ys.Playing)
                return 1;
            return xs.CompareTo(y);
        }
    }
}