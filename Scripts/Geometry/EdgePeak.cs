using System;

namespace Triangulation
{
    public struct EdgePeak
    {
        public static readonly EdgePeak None = new EdgePeak(false);

        public bool IsValid { get; set; }
        public PeakRect PeakRect { get; private set; }
        public EdgeEntry EdgeA { get; private set; }
        public EdgeEntry EdgeB { get; private set; }
        public Vector2 EdgeVecA { get; private set; }
        public Vector2 EdgeVecB { get; private set; }
        public int AngleSign { get; private set; }
        public float Angle { get; private set; }
        public int PeakVertex { get; private set; }
        public int VertexA => EdgeA.GetOtherVertex(PeakVertex);
        public int VertexB => EdgeB.GetOtherVertex(PeakVertex);
        public bool IsConvex => Angle <= 180f;

        public EdgePeak(bool valid = false) : this()
        {
            IsValid = valid;
        }

        public EdgePeak(EdgeEntry edge1, EdgeEntry edge2, Vector2[] points) : this()
        {
            IsValid = true;
            EdgeA = edge1;
            EdgeB = edge2;
            PeakVertex = EdgeEntry.GetSharedVertex(edge1, edge2);
            if (PeakVertex < 0)
            {
                throw new Exception("EdgePeak: sharedIndex: " + PeakVertex + " " + edge1 + " " + edge2);
            }
            EdgeVecA = edge1.GetVector(points, PeakVertex == edge1.B); // PeakVertex to VertexA vector
            EdgeVecB = edge2.GetVector(points, PeakVertex == edge2.B); // PeakVertex to VertexB vector
            float cross = Vector2.Cross(EdgeVecB, EdgeVecA);
            AngleSign = MathF.Sign(cross);
        }

        public EdgePeak Invalidate()
        {
            IsValid = false;
            return this;
        }

        public EdgePeak Setup(Vector2[] points)
        {
            PeakRect = new PeakRect(this, points);
            return this;
        }

        public EdgePeak Setup(int innerAngleSign, Vector2[] points)
        {
            Setup(points);
            SetAngle360(innerAngleSign);
            return this;
        }

        public float InvertAngle()
        {
            Angle = 360f - Angle;
            return Angle;
        }

        public float SetAngle360()
        {
            return SetAngle360(AngleSign < 0);
        }

        public EdgeEntry GetOtherEdge(EdgePeak otherPeak)
        {
            if (EdgeA.Equals(otherPeak.EdgeA) || EdgeA.Equals(otherPeak.EdgeB))
            {
                return EdgeB;
            }
            else if (EdgeB.Equals(otherPeak.EdgeA) || EdgeB.Equals(otherPeak.EdgeB))
            {
                return EdgeA;
            }
            else
            {
                throw new Exception("GetOtherEdge: " + this + " " + otherPeak);
            }
        }

        public EdgeEntry GetOppositeEdge(out bool abOrder)
        {
            int a = EdgeA.GetOtherVertex(PeakVertex);
            int b = EdgeB.GetOtherVertex(PeakVertex);
            abOrder = a > b;
            return new EdgeEntry(a, b);
        }

        public bool MakesDegenerateTriangle(Vector2[] points, Vector2[] edgeBuffer)
        {
            var abEdge = GetOppositeEdge(out bool abOrder);
            var abEdgeVec = abEdge.GetVector(points);
            edgeBuffer[0] = EdgeVecA.Normalized();
            edgeBuffer[1] = EdgeVecB.Normalized();
            edgeBuffer[2] = abEdgeVec.Normalized();
            int reverseIndex = abOrder ? 1 : 0;
            edgeBuffer[reverseIndex] = -edgeBuffer[reverseIndex];
            return Triangle.IsDegenerate(edgeBuffer, true);
        }

        public Triangle CreateTriangle(Vector2[] points)
        {
            int a = EdgeA.GetOtherVertex(PeakVertex);
            int b = EdgeB.GetOtherVertex(PeakVertex);
            return new Triangle(PeakVertex, a, b, points);
        }

        public override string ToString()
        {
            return string.Format("EdgePeak: " + EdgeA.ToShortString() + EdgeB.ToShortString() + " Angle: " + Angle.ToString("f2") + " " + IsValid);
        }

        private void SetAngle360(int innerAngleSign)
        {
            SetAngle360(innerAngleSign != AngleSign);
        }

        private float SetAngle360(bool inverted)
        {
            Angle = Vector2.AngleDeg(EdgeVecA, EdgeVecB);
            return inverted ? InvertAngle() : Angle;
        }
    }
}
