using System;
using System.Numerics;

namespace Triangulation
{
    public struct EdgeEntry
    {
        public static readonly EdgeEntry None = new EdgeEntry()
        {
            A = -1,
            B = -1,
            Count = 0
        };
        public static float DegenerateDistance { set => DegenerateDistanceSqr = value * value; }
        private static float DegenerateDistanceSqr;

        public bool IsValid => Count > 0 && A != B && A >= 0 && B >= 0;
        public bool IsTerminal => Prev < 0 || Next < 0;

        public bool LastPointOpposite { get; set; }
        public bool LastPointDegenerateTriangle { get; private set; }
        public bool LastPointDegenerateAngle { get; private set; }
        public bool LastPointInRange { get; private set; }

        public int Next;
        public int Prev;
        public int A;
        public int B;
        public int Count;

        public static int GetLastElementCount(EdgeEntry[] edges)
        {
            return edges[^1].Count;
        }

        public static void SetLastElementCount(EdgeEntry[] edges, int count)
        {
            edges[^1].Count = count;
        }

        public static void RefreshSortedEdgesNextPrev(EdgeEntry[] edges, int count, int startIndex = 0)
        {
            for (int i = startIndex; i < count; i++)
            {
                edges[i].Prev = i - 1;
                edges[i].Next = i + 1;
            }
            edges[0].Prev = count - 1;
            edges[count - 1].Next = 0;
        }

        public static bool GetSharedVertex(EdgeEntry edge1, EdgeEntry edge2, out int vertex)
        {
            return (vertex = GetSharedVertex(edge1, edge2)) >= 0;
        }

        public static int GetSharedVertex(EdgeEntry edge1, EdgeEntry edge2)
        {
            return edge2.HasVertex(edge1.A) ? edge1.A : (edge2.HasVertex(edge1.B) ? edge1.B : -1);
        }

        public EdgeEntry(int a, int b) : this()
        {
            if (a == b)
            {
                throw new ArgumentException(string.Format("EdgeEntry: ({0}, {1})", a, b));
            }
            (A, B) = a > b ? (a, b) : (b, a);

            Count = 1;
            Next = Prev = -1;
        }

        public void ClearLastPointDegenerate()
        {
            LastPointDegenerateTriangle = false;
            LastPointDegenerateAngle = false;
        }

        public void ClearLastPointData()
        {
            LastPointOpposite = false;
            LastPointDegenerateTriangle = false;
            LastPointDegenerateAngle = false;
            LastPointInRange = false;
        }

        public bool IsPointInRange(int pointIndex, Vector2[] points)
        {
            var pointRay = points[pointIndex] - points[A];
            var edge = GetNormalizedVector(points, out float edgeLength);
            float dotRayEdge = Vector2.Dot(pointRay, edge);
            return dotRayEdge > 0f && dotRayEdge < edgeLength;
        }

        public bool SetLastPointOnEgdeData(Vector2 point, Vector2[] points, out bool inRange)
        {
            bool onEdge = IsPointOnEdge(point, points, true, out inRange);
            return onEdge;
        }

        public bool IsPointOnEdge(Vector2 point, Vector2[] points, bool setLastPointData, out bool inRange)
        {
            var pointRayA = point - points[A];
            var pointRayB = point - points[B];

            var edge = GetNormalizedVector(points, out float edgeLength);
            float dotRayEdgeA = Vector2.Dot(pointRayA, edge);
            inRange = dotRayEdgeA > 0f && dotRayEdgeA < edgeLength;

            //var pointRayN = pointRayA - dotRayEdgeA * edge;
            //float distSqr = pointRayN.LengthSquared();
            //bool onLine = distSqr < DegenerateDistanceSqr;

            pointRayA = pointRayA.Normalized();
            pointRayB = pointRayB.Normalized();
            float cosAngleA = Vector2.Dot(pointRayA, edge);
            float cosAngleB = Vector2.Dot(pointRayB, -edge);
            float cosAngleC = Vector2.Dot(-pointRayA, -pointRayB);

            bool degenerateAngleA = MathF.Abs(cosAngleA) > Triangle.CosMinAngle;
            bool degenerateAngleB = MathF.Abs(cosAngleB) > Triangle.CosMinAngle;
            bool degenerateAngleC = cosAngleC < -Triangle.CosMinAngle;

            bool onEdge = inRange && ((degenerateAngleA && degenerateAngleB) || degenerateAngleC);

            if (setLastPointData)
            {
                LastPointDegenerateTriangle = degenerateAngleA || degenerateAngleB || degenerateAngleC;
                LastPointInRange = inRange;
                // LastPointDegenerateAngle
                {
                    var bisector = -(pointRayA + pointRayB).Normalized();
                    bool degenerateDistA = points[A].GetSqrDistToLine(point, bisector) < DegenerateDistanceSqr;
                    bool degenerateDistB = points[B].GetSqrDistToLine(point, bisector) < DegenerateDistanceSqr;
                    LastPointDegenerateAngle = degenerateDistA || degenerateDistB;

                    if (!LastPointDegenerateAngle && LastPointOpposite && LastPointInRange)
                    {
                        LastPointDegenerateTriangle = false;
                    }
                }
            }
            return onEdge;
        }

        public void SetLastPointData(EdgeEntry other)
        {
            LastPointOpposite = other.LastPointOpposite;
            LastPointDegenerateAngle = other.LastPointDegenerateAngle;
            LastPointDegenerateTriangle = other.LastPointDegenerateTriangle;
            LastPointInRange = other.LastPointInRange;
        }

        public Vector2 GetNormalizedVector(Vector2[] points, out float edgeLength)
        {
            var edge = GetVector(points);
            edgeLength = edge.Length();
            if (edgeLength > Mathv.Epsilon)
            {
                edge /= edgeLength;
            }
            return edge;
        }

        public Vector2 GetVector(Vector2[] points)
        {
            return points[B] - points[A];
        }

        public Vector2 GetVector(Vector2[] points, bool opposite)
        {
            return opposite ? (points[A] - points[B]) : (points[B] - points[A]);
        }

        public Vector2 GetMidPoint(Vector2[] points)
        {
            return (points[A] + points[B]) * 0.5f;
        }

        public int GetOtherVertex(int index)
        {
            return A == index ? B : (B == index ? A : -1);
        }

        public void SwapNextPrev()
        {
            (Prev, Next) = (Next, Prev);
        }

        public bool HasVertex(int index)
        {
            return A == index || B == index;
        }

        public bool SharesVertex(EdgeEntry other)
        {
            return HasVertex(other.A) || HasVertex(other.B);
        }

        public bool Equals(int entryA, int entryB)
        {
            return (A == entryA && B == entryB) || (A == entryB && B == entryA);
        }

        public bool Equals(EdgeEntry entry)
        {
            return A == entry.A && B == entry.B;
        }

        public string ToShortString()
        {
            return string.Format("({0}, {1})", A, B);
        }

        public string ToLastPointDataString(bool withDegenerates)
        {
            if (withDegenerates)
            {
                return string.Format("({0}, {1}) | opposite: {2} | inRange: {3} | degenerateTriangle: {4} | degenerateAngle: {5}", A, B, LastPointOpposite, LastPointInRange, LastPointDegenerateTriangle, LastPointDegenerateAngle);
            }
            else
            {
                return string.Format("({0}, {1}) | opposite: {2} | inRange: {3}", A, B, LastPointOpposite, LastPointInRange);
            }
        }

        public override string ToString()
        {
            return string.Format("({0}, {1} count: {2})", A, B, Count);
            //return string.Format("({0}, {1} count: {2}, prev: {3}, next: {4})", A, B, Count, Prev, Next);
        }
    }

    public struct EdgeBucketEntry
    {
        public int generation;
        public int entryIndex;
    }
}
