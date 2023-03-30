using System;
using System.Drawing;

namespace Triangulation
{
    public struct Triangle
    {
        public const float CircumCircleTolerance = 0.00001f;

        public static readonly Triangle None = new Triangle(-1, -1, -1);

        public static float CosMinAngle { get; private set; } = Maths.Cos1Deg;

        private static readonly Vector2[] rayBuffer = new Vector2[3];
        private static readonly Vector2[] edgeBuffer = new Vector2[3];
        private static readonly Vector2[] vertsBuffer = new Vector2[3];
        private static readonly float[] crossBuffer = new float[3];
        private static readonly int[] signBuffer = new int[3];

        public long Key { get; private set; }

        public float CircumCircleMinX => CircumCircle.Bounds.min.x;
        public float CircumCircleMaxX => CircumCircle.Bounds.max.x;

        public int A;
        public int B;
        public int C;

        public Color FillColor;
        public Circle CircumCircle;

        public int Previous;
        public int Next;

        public int PrevNonCompleted;
        public int NextNonCompleted;

        //public static void SetKeys(Triangle[] triangles, int trianglesCount, int pointsLength, int[] indexBuffer)
        //{
        //    for (int i = 0; i < trianglesCount; i++)
        //    {
        //        triangles[i].SetKey(pointsLength, indexBuffer);
        //    }
        //}

        public static bool IsDegenerate(int a, int b, int c, Vector2[] points)
        {
            edgeBuffer[0] = (points[b] - points[a]).Normalized();
            edgeBuffer[1] = (points[c] - points[b]).Normalized();
            edgeBuffer[2] = (points[a] - points[c]).Normalized();
            return IsDegenerate(edgeBuffer, true);
        }

        public static bool IsDegenerate(Vector2[] edges, bool normalized)
        {
            if (normalized)
            {
                for (int i = 0; i < 3; i++)
                {
                    edgeBuffer[i] = edges[i];
                }
            }
            else
            {
                for (int i = 0; i < 3; i++)
                {
                    edgeBuffer[i] = edges[i].Normalized();
                }
            }
            for (int i = 0; i < 3; i++)
            {
                float absCosAngle = MathF.Abs(Vector2.Dot(-edgeBuffer[i], edgeBuffer[(i + 1) % 3]));
                //Log.WriteLine("Triangle.IsDegenerate: angle: " + (MathF.Acos(absCosAngle) * SPMathF.Rad2Deg));
                if (absCosAngle > CosMinAngle)
                {
                    return true;
                }
            }
            return false;
        }

        public static void SetMinAngle(float angleInDeg)
        {
            CosMinAngle = MathF.Cos(angleInDeg * Maths.Deg2Rad);
        }

        public static Triangle GetTriangleFromKey(long key, long pointsLength)
        {
            long ptsLengthSqr = pointsLength * pointsLength;
            long a = key / ptsLengthSqr;
            long bc = key % ptsLengthSqr;
            long b = bc / pointsLength;
            long c = bc % pointsLength;
            var t = new Triangle((int)a, (int)b, (int)c) {
                Key = key
            };
            return t;
        }

        public Triangle(int a, int b, int c) : this()
        {
            SetIndices(a, b, c);
            ClearKey();
        }

        public Triangle(int a, int b, int c, Vector2[] points) : this()
        {
            Setup(a, b, c, points);
            ClearKey();
        }

        #region ToIntegerTriangle

        //public IntegerTriangle ToIntegerTriangle()
        //{
        //    IntegerTriangle result;
        //    result.A = A;
        //    result.B = B;
        //    result.C = C;
        //    return result;
        //}

        //public void ToIntegerTriangle(ref IntegerTriangle destination)
        //{
        //    destination.A = A;
        //    destination.B = B;
        //    destination.C = C;
        //}

        #endregion

        public void ClearKey()
        {
            Key = -1;
        }

        public long SetKey(long pointsLength, int[] indexBuffer)
        {
            SortIndices(indexBuffer);
            Key = pointsLength * pointsLength * A + pointsLength * B + C;
            //if (Key < 0)
            //{
            //    throw new Exception(ToString());
            //}
            return Key;
        }

        public void SortIndices(int[] indexBuffer)
        {
            GetIndices(indexBuffer);
            Array.Sort(indexBuffer);
            SetIndices(indexBuffer[0], indexBuffer[1], indexBuffer[2]);
            //Array.Sort(indexBuffer, (a, b) => b.CompareTo(a));
        }

        public bool ContainsPoint(Vector2 point, Vector2[] points, bool log = false)
        {
            GetEdges(points, edgeBuffer);
            GetVerts(points, vertsBuffer);
            for (int i = 0; i < 3; i++)
            {
                rayBuffer[i] = point - vertsBuffer[i];
            }
            crossBuffer[0] = Vector2.Cross(rayBuffer[2], edgeBuffer[2]);
            crossBuffer[1] = Vector2.Cross(rayBuffer[2], edgeBuffer[1]);
            crossBuffer[2] = Vector2.Cross(rayBuffer[0], edgeBuffer[0]);
            int signCount = 0;
            int sign;
            if (log)
            {
                Log.WriteLine(GetType() + ".ContainsPoint:");
            }
            for (int i = 0; i < 3; i++)
            {
                sign = MathF.Sign(crossBuffer[i]);
                if (log)
                {
                    Log.WriteLine(GetType() + ".ContainsPoint: " + crossBuffer[i] + " " + sign);
                }
                if (sign != 0)
                {
                    signBuffer[signCount++] = sign;
                }
            }
            if (signCount == 0)
            {
                throw new Exception(GetType() + ".ContainsPoint: signCount == 0");
            }
            sign = signBuffer[0];
            for (int i = 1; i < signCount; i++)
            {
                if (sign != signBuffer[i])
                {
                    return false;
                }
            }
            return true;
        }

        public float GetArea(Vector2[] points)
        {
            return 0.5f * MathF.Abs(Vector2.Cross(points[A] - points[C], points[B] - points[C]));
        }

        public float GetOppositeAngleDeg(EdgeEntry edge, Vector2[] points)
        {
            return GetOppositeAngleRad(edge, points) * Maths.Rad2Deg;
        }

        public float GetOppositeAngleRad(EdgeEntry edge, Vector2[] points)
        {
            int vertIndex = GetOppositeVertex(edge);
            if (vertIndex == A)
            {
                return Vector2.AngleRad(points[C] - points[A], points[B] - points[A]);
            }
            else if (vertIndex == B)
            {
                return Vector2.AngleRad(points[C] - points[B], points[A] - points[B]);
            }
            else if (vertIndex == C)
            {
                return Vector2.AngleRad(points[A] - points[C], points[B] - points[C]);
            }
            else
            {
                throw new Exception("GetOppositeAngleRad: " + edge + " vertIndex: " + vertIndex);
            }
        }

        public void GetOtherEdges(EdgeEntry edge, EdgeEntry[] edges, out int oppVertIndex)
        {
            oppVertIndex = GetOppositeVertex(edge);
            GetEdgesWithVertex(oppVertIndex, edges);
        }

        public void GetEdgesWithVertex(int vertIndex, EdgeEntry[] edges)
        {
            if (vertIndex == A)
            {
                //edge1 = new EdgeEntry(C, A);
                //edge2 = new EdgeEntry(A, B);
                GetEdges(edges, 1);
            }
            else if (vertIndex == B)
            {
                //edge1 = new EdgeEntry(A, B);
                //edge2 = new EdgeEntry(B, C);
                GetEdges(edges, 0);
            }
            else if (vertIndex == C)
            {
                //edge1 = new EdgeEntry(B, C);
                //edge2 = new EdgeEntry(C, A);
                GetEdges(edges, 2);
            }
            else
            {
                edges[0] = edges[1] = EdgeEntry.None;
            }
            edges[2] = EdgeEntry.None;
        }

        public EdgeEntry GetOppositeEdge(int vertIndex)
        {
            if (vertIndex == A)
            {
                return new EdgeEntry(B, C);
            }
            else if (vertIndex == B)
            {
                return new EdgeEntry(C, A);
            }
            else if (vertIndex == C)
            {
                return new EdgeEntry(A, B);
            }
            else
            {
                return EdgeEntry.None;
            }
        }

        public int GetOppositeVertex(EdgeEntry edge)
        {
            if (!HasEdge(edge))
            {
                throw new Exception("GetOppositeVertex: " + this + " !HasEdge " + edge);
            }
            if (A != edge.A && A != edge.B)
            {
                return A;
            }
            else if (B != edge.A && B != edge.B)
            {
                return B;
            }
            else
            {
                return C;
            }
        }

        public Vector2 GetOppositeVertex(EdgeEntry edge, Vector2[] points, out int vertIndex)
        {
            return points[vertIndex = GetOppositeVertex(edge)];
        }

        public bool HasEdge(EdgeEntry edge)
        {
            if (edge.HasVertex(A))
            {
                return edge.HasVertex(B) || edge.HasVertex(C);
            }
            else
            {
                return edge.HasVertex(B) && edge.HasVertex(C);
            }
        }

        public bool HasVertex(int index)
        {
            return A == index || B == index || C == index;
        }

        public void GetIndices(int[] indexBuffer, int offset)
        {
            indexBuffer[offset++] = A;
            indexBuffer[offset++] = B;
            indexBuffer[offset++] = C;
        }

        public void GetIndices(int[] indexBuffer)
        {
            indexBuffer[0] = A;
            indexBuffer[1] = B;
            indexBuffer[2] = C;
        }

        public void GetVerts(Vector2[] points, Vector2[] vertsBuffer)
        {
            vertsBuffer[0] = points[A];
            vertsBuffer[1] = points[B];
            vertsBuffer[2] = points[C];
        }

        public void GetEdges(EdgeEntry[] edges, int offset)
        {
            edges[(0 + offset) % 3] = new EdgeEntry(A, B);
            edges[(1 + offset) % 3] = new EdgeEntry(B, C);
            edges[(2 + offset) % 3] = new EdgeEntry(C, A);
        }

        public void GetEdges(EdgeEntry[] edges)
        {
            edges[0] = new EdgeEntry(A, B);
            edges[1] = new EdgeEntry(B, C);
            edges[2] = new EdgeEntry(C, A);
        }

        public void GetEdges(Vector2[] points, Vector2[] edgeBuffer)
        {
            edgeBuffer[0] = points[B] - points[A];
            edgeBuffer[1] = points[C] - points[B];
            edgeBuffer[2] = points[A] - points[C];
        }

        public void GetNormalizedEdges(Vector2[] points, Vector2[] edgeBuffer)
        {
            edgeBuffer[0] = (points[B] - points[A]).Normalized();
            edgeBuffer[1] = (points[C] - points[B]).Normalized();
            edgeBuffer[2] = (points[A] - points[C]).Normalized();
        }

        public Vector2 GetMidPoint(Vector2[] points)
        {
            return (points[A] + points[B] + points[C]) / 3f;
        }

        public void ClearNotCompleted()
        {
            PrevNonCompleted = -1;
            NextNonCompleted = -1;
        }

        public void ClearPrevNext()
        {
            Previous = -1;
            Next = -1;
            ClearNotCompleted();
        }

        public void Setup(int a, int b, int c, Vector2[] points)
        {
            SetIndices(a, b, c);
            ComputeCircumCircle(points[A], points[B], points[C]);
            FillColor = Color.FloralWhite;
        }

        public void SetIndices(int a, int b, int c)
        {
            A = a;
            B = b;
            C = c;
        }

        public void ComputeCircumCircle(Vector2 a, Vector2 b, Vector2 c)
        {
            float A = b.x - a.x,
                B = b.y - a.y,
                C = c.x - a.x,
                D = c.y - a.y,
                E = A * (a.x + b.x) + B * (a.y + b.y),
                F = C * (a.x + c.x) + D * (a.y + c.y),
                G = 2f * (A * (c.y - b.y) - B * (c.x - b.x)),
                minx, miny, dx, dy;
            float cX, cY, sqrR;

            /* If the points of the triangle are collinear, then just find the
             * extremes and use the midpoint as the center of the circumcircle. */
            if (MathF.Abs(G) < CircumCircleTolerance)
            {
                minx = MathF.Min(MathF.Min(a.x, b.x), c.x);
                miny = MathF.Min(MathF.Min(a.y, b.y), c.y);
                dx = (MathF.Max(MathF.Max(a.x, b.x), c.x) - minx) * 0.5f;
                dy = (MathF.Max(MathF.Max(a.y, b.y), c.y) - miny) * 0.5f;

                cX = minx + dx;
                cY = miny + dy;
                sqrR = dx * dx + dy * dy;

                //Log.WriteWarning(GetType() + ".ComputeCircumCircle: collinear: " + G + " " + cX + " " + cY + " " + sqrR + " | " + a + " " + b + " " + c);
                Log.WriteWarning(GetType() + ".ComputeCircumCircle: collinear: " + this);
            }
            else
            {
                cX = (D * E - B * F) / G;
                cY = (A * F - C * E) / G;
                dx = cX - a.x;
                dy = cY - a.y;
                sqrR = dx * dx + dy * dy;
            }
            CircumCircle = new Circle(new Vector2(cX, cY), sqrR);
        }

        public bool HasSameSortedIndices(Triangle t)
        {
            return A == t.A && B == t.B && C == t.C;
        }

        public string ToString(Vector2[] points)
        {
            return string.Format("({0}, {1}, {2}), ({3}, {4}, {5}) Key: {6}", A, B, C, points[A], points[B], points[C], Key);
        }

        public override string ToString()
        {
            //return string.Format("({0}, {1}, {2}) : {3}, {4} | {5}, {6} | {7}", A, B, C, Previous, Next, prevNonCompleted, nextNonCompleted, CircumCircle);
            //return string.Format("({0}, {1}, {2}) | {3} | Key: {4}", A, B, C, CircumCircle, Key);
            //return string.Format("({0}, {1}, {2}) Key: {3}", A, B, C, Key);
            return string.Format("({0}, {1}, {2})", A, B, C);
        }
    }
}
