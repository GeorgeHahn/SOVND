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

            return xs.CompareTo(y);
        }
    }
}