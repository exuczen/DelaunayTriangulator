using System;
using System.Collections.Generic;

namespace Triangulation
{
    public struct EdgeComparer : IComparer<EdgeEntry>
    {
        public int Compare(EdgeEntry edge1, EdgeEntry edge2)
        {
            if (edge1.A != edge2.A)
            {
                return MathF.Sign(edge2.A - edge1.A);
            }
            if (edge1.B != edge2.B)
            {
                return MathF.Sign(edge2.B - edge1.B);
            }
            return 0;
        }
    }
}
