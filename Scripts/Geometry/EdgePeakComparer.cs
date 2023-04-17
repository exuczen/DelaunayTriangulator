using System.Collections.Generic;

namespace Triangulation
{
    public readonly struct EdgePeakComparer : IComparer<EdgePeak>
    {
        public int Compare(EdgePeak a, EdgePeak b)
        {
            return a.ValueToCompare.CompareTo(b.ValueToCompare);
        }
    }
}
