namespace Triangulation
{
    public struct PeakRect
    {
        public Vector2 N1 { private set; get; }
        public Vector2 N2 { private set; get; }
        public Vector2 Size { private set; get; }
        public Vector2 Origin { private set; get; }

        public PeakRect(EdgePeak peak, Vector2[] points) : this()
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
        }

        public override string ToString()
        {
            return string.Format("PeakRect: {0}{1}{2}{3}", N1, N2, Size, Origin);
        }

        public bool ContainsPoint(Vector2 point, float tolerance)
        {
            var ray = point - Origin;
            float a = Vector2.Dot(ray, N1);
            float b = Vector2.Dot(ray, N2);
            var bounds = new Bounds2(-tolerance * Vector2.One, Size + tolerance * Vector2.One);
            return bounds.Contains(new Vector2(a, b));
        }
    }
}
