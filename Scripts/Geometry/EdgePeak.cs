using System;
using System.Numerics;

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
        public float ValueToCompare { get; private set; }
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
            float cross = Mathv.Cross(EdgeVecB, EdgeVecA);
            AngleSign = MathF.Sign(cross);
        }

        public EdgePeak Invalidate()
        {
            IsValid = false;
            return this;
        }

        public EdgePeak SetupPeakRect(int innerAngleSign, Vector2[] points)
        {
            PeakRect = new PeakRect(this, points, innerAngleSign);
            return this;
        }

        public EdgePeak Setup(int innerAngleSign, Vector2[] points)
        {
            SetupPeakRect(innerAngleSign, points);
            SetAngle360(innerAngleSign);
            return this;
        }

        public EdgePeak SetValueToCompare(float value)
        {
            ValueToCompare = value;
            return this;
        }

        public float CosAngle()
        {
            return Vector2.Dot(EdgeVecA.Normalized(), EdgeVecB.Normalized());
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

        public float GetPointRayAngleB(Vector2 point, Vector2[] points)
        {
            var pointRay = point - points[PeakVertex];
            int signA = MathF.Sign(Mathv.Cross(pointRay, EdgeVecA));
            int signB = MathF.Sign(Mathv.Cross(pointRay, EdgeVecB));
            pointRay = pointRay.Normalized();
            float cosB = Vector2.Dot(pointRay, EdgeVecB.Normalized());
            float angle = MathF.Acos(cosB);

            if (signA == signB)
            {
                float cosA = Vector2.Dot(pointRay, EdgeVecA.Normalized());
                if (cosB < cosA)
                {
                    angle = MathF.Tau - angle;
                }
            }
            return angle * Maths.Rad2Deg;
        }

        //public bool IsPointInAngularRange(Vector2 point, Vector2[] points)
        //{
        //    if (AngleSign == 0)
        //    {
        //        return false;
        //    }
        //    var pointRay = point - points[PeakVertex];
        //    int signA = MathF.Sign(Mathv.Cross(pointRay, EdgeVecA));
        //    int signB = MathF.Sign(Mathv.Cross(pointRay, EdgeVecB));
        //    if (signA == 0 || signB == 0 || signA == signB)
        //    {
        //        return false;
        //    }
        //    else
        //    {
        //        var bisector = EdgeVecA.Normalized() + EdgeVecB.Normalized();
        //        return Vector2.Dot(pointRay, bisector) > 0f;
        //    }
        //}

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

        public string ToLongString()
        {
            return string.Format("{0} | {1}", ToString(), ValueToCompare);
        }

        public override string ToString()
        {
            return string.Format("{0}{1} Angle: {2} | {3}", EdgeA.ToShortString(), EdgeB.ToShortString(), Angle.ToString("f2"), IsValid);
        }

        private void SetAngle360(int innerAngleSign)
        {
            SetAngle360(innerAngleSign != AngleSign);
        }

        private float SetAngle360(bool inverted)
        {
            Angle = Mathv.AngleDeg(EdgeVecA, EdgeVecB);
            return inverted ? InvertAngle() : Angle;
        }
    }
}
