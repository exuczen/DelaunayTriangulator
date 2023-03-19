using System;

namespace Triangulation
{
    public struct EdgeEntry
    {
        public static readonly EdgeEntry None = new EdgeEntry(-1, -1) { Count = 0 };

        public float SqrLength { get; private set; }

        public bool IsValid => Count > 0 && A != B && A >= 0 && B >= 0;
        public bool IsTerminal => Prev < 0 || Next < 0;

        public bool LastPointDegenerateTriangle;
        public bool LastPointDegenerateAngle;
        public bool LastPointInRange;

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

        public static int GetSharedVertex(EdgeEntry edge1, EdgeEntry edge2)
        {
            return edge2.HasVertex(edge1.A) ? edge1.A : (edge2.HasVertex(edge1.B) ? edge1.B : -1);
        }

        public EdgeEntry(int a, int b) : this()
        {
            (A, B) = a > b ? (a, b) : (b, a);

            Count = 1;
            Next = Prev = -1;
        }

        public void SetSqrLength(Vector2[] points)
        {
            SqrLength = GetVector(points).SqrLength;
        }

        public void ClearLastPointData()
        {
            LastPointDegenerateTriangle = false;
            LastPointDegenerateAngle = false;
            LastPointInRange = false;
        }

        //public bool IsPointOnEdge(int pointIndex, Vector2[] points, out bool inRange)
        //{
        //    return IsPointOnEdge(points[pointIndex], points, out inRange, false);
        //}

        //public bool IsPointOnEdge(Vector2 point, Vector2[] points, out bool inRange)
        //{
        //    return IsPointOnEdge(point, points, out inRange, false);
        //}

        public bool IsPointInRange(int pointIndex, Vector2[] points)
        {
            var pointRay = points[pointIndex] - points[A];
            var edge = GetNormalizedVector(points, out float edgeLength);
            float dotRayEdge = Vector2.Dot(pointRay, edge);
            return dotRayEdge > 0f && dotRayEdge < edgeLength;
        }

        public bool SetLastPointOnEgdeData(Vector2 point, Vector2[] points, out bool inRange)
        {
            return IsPointOnEdge(point, points, out inRange, true);
        }

        public bool IsPointOnEdge(Vector2 point, Vector2[] points, out bool inRange, bool setLastPointData)
        {
            var pointRayA = point - points[A];
            var pointRayB = point - points[B];

            var edge = GetNormalizedVector(points, out float edgeLength);
            float dotRayEdgeA = Vector2.Dot(pointRayA, edge);
            inRange = dotRayEdgeA > 0f && dotRayEdgeA < edgeLength;

            //var pointRayNormal = pointRayA - dotRayEdgeA * edge;
            //float sqrDist = pointRayNormal.SqrLength;
            //bool onLine = sqrDist < pointOnEdgeSqrDistMin;

            pointRayA = pointRayA.Normalized();
            pointRayB = pointRayB.Normalized();
            float cosAngleA = Vector2.Dot(pointRayA, edge);
            float cosAngleB = Vector2.Dot(pointRayB, -edge);
            float cosAngleC = Vector2.Dot(-pointRayA, -pointRayB);

            bool degenerateAngleA = MathF.Abs(cosAngleA) > Triangle.CosMinAngle;
            bool degenerateAngleB = MathF.Abs(cosAngleB) > Triangle.CosMinAngle;
            bool degenerateAngleC = MathF.Abs(cosAngleC) > Triangle.CosMinAngle;
            bool onEdge = inRange && ((degenerateAngleA && degenerateAngleB) || (cosAngleC < -Triangle.CosMinAngle));

            if (setLastPointData)
            {
                LastPointDegenerateTriangle = degenerateAngleA || degenerateAngleB || degenerateAngleC;
                //LastPointDegenerateAngle = cosAngleC > Triangle.CosMinAngle;
                LastPointDegenerateAngle = cosAngleC > Triangle.CosMinAngle || (!inRange && (degenerateAngleA || degenerateAngleB));
                LastPointInRange = inRange;
                //Log.WriteLine(GetType() + ".IsPointOnEdge: " + ToLastPointDataString());
            }
            return onEdge;
        }

        public Vector2 GetNormalizedVector(Vector2[] points, out float edgeLength)
        {
            var edge = GetVector(points);
            edgeLength = edge.Length;
            if (edgeLength > Vector2.Epsilon)
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

        public string ToLastPointDataString()
        {
            return string.Format("({0}, {1}), degenerateTriangle: {2}, degenerateAngle: {3}, inRange: {4}", A, B, LastPointDegenerateTriangle, LastPointDegenerateAngle, LastPointInRange);
        }

        public override string ToString()
        {
            return string.Format("({0}, {1} count: {2})", A, B, Count);
            //return string.Format("({0}, {1} count: {2}, prev: {3}, next: {4})", A, B, Count, Prev, Next);
            //return string.Format("({0}, {1} count: {2}, {3}, {4})", A, B, Count, LastPointOnEdge, LastPointDegenerateTriangle);
        }
    }

    public struct EdgeBucketEntry
    {
        public int generation;
        public int entryIndex;
    }
}
