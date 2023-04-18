using System;
using System.Numerics;

namespace Triangulation
{
    public struct PeakRect
    {
        public Vector2 N1 { private set; get; }
        public Vector2 N2 { private set; get; }
        public Vector2 Size { private set; get; }
        public Vector2 Origin { private set; get; }

        public PeakRect(EdgePeak peak, Vector2[] points, int innerAngleSign) : this()
        {
            //var edgeA = peak.EdgeA;
            var edgeVecA = -peak.EdgeVecA;
            var oppEdge = peak.GetOppositeEdge(out bool abOrder);
            var oppEdgeVec = oppEdge.GetVector(points, !abOrder);

            Origin = points[abOrder ? oppEdge.A : oppEdge.B];

            N1 = oppEdgeVec.Normalized(out float n1Length);
            float dot_edgeA_N1 = Vector2.Dot(edgeVecA, N1);
            var edgeA_N1 = dot_edgeA_N1 * N1;
            var edgeA_N2 = edgeVecA - edgeA_N1;
            N2 = edgeA_N2.Normalized(out float n2Length);

            if (dot_edgeA_N1 < 0f)
            {
                Origin += edgeA_N1;
                n1Length -= dot_edgeA_N1;
            }
            else if (dot_edgeA_N1 > n1Length)
            {
                n1Length = dot_edgeA_N1;
            }
            Size = new Vector2(n1Length, n2Length);

            bool n1zero = n1Length < Mathv.Epsilon;
            bool n2zero = n2Length < Mathv.Epsilon;
            if (n1zero)
            {
                if (n2zero)
                {
                    throw new Exception("PeakRect: n1zero && n2zero: " + peak);
                }
                N1 = new Vector2(-N2.Y, N2.X) * innerAngleSign;
            }
            else if (n2zero)
            {
                N2 = new Vector2(N1.Y, -N1.X) * innerAngleSign;
            }
        }

        public override string ToString()
        {
            return string.Format("PeakRect: {0}{1}{2}{3}", N1.ToStringF2(), N2.ToStringF2(), Size.ToStringF2(), Origin.ToStringF2());
        }

        public bool ContainsPoint(Vector2 point, float tolerance)
        {
            var ray = point - Origin;
            float a = Vector2.Dot(ray, N1);
            float b = Vector2.Dot(ray, N2);
            var bounds = Bounds2.MinMax(-tolerance * Vector2.One, Size + tolerance * Vector2.One);
            return bounds.Contains(new Vector2(a, b));
        }
    }
}
