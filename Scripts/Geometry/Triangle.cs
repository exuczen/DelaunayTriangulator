using System;
using System.Drawing;
using System.Numerics;

namespace Triangulation
{
    public struct Triangle
    {
        public const float CircumCircleTolerance = 0.00001f;

        public static readonly Triangle None = new Triangle(-1, -1, -1);

        public static readonly Color DefaultColor = Color.FloralWhite;

        public static float CosMinAngle { get; private set; } = Maths.Cos1Deg;

        private static readonly Vector2[] rayBuffer = new Vector2[3];
        private static readonly Vector2[] edgeVecBuffer = new Vector2[3];
        private static readonly Vector2[] vertsBuffer = new Vector2[3];
        private static readonly float[] crossBuffer = new float[3];
        private static readonly int[] signBuffer = new int[3];
        //private static readonly EdgeEntry[] edgeBuffer = new EdgeEntry[3];
        //private static readonly int[] indexBuffer = new int[3];

        public long Key { get; private set; }

        public bool IsNone => A < 0 || B < 0 || C < 0;

        public float CircumCircleMinX => CircumCircle.Bounds.min.X;
        public float CircumCircleMaxX => CircumCircle.Bounds.max.X;

        public int A;
        public int B;
        public int C;

        public Color FillColor;
        public Circle CircumCircle;

        public int Previous;
        public int Next;

        public int PrevNonCompleted;
        public int NextNonCompleted;

        public static bool IsDegenerate(Vector2[] edges, bool normalized)
        {
            if (normalized)
            {
                for (int i = 0; i < 3; i++)
                {
                    edgeVecBuffer[i] = edges[i];
                }
            }
            else
            {
                for (int i = 0; i < 3; i++)
                {
                    edgeVecBuffer[i] = edges[i].Normalized();
                }
            }
            for (int i = 0; i < 3; i++)
            {
                float absCosAngle = MathF.Abs(Vector2.Dot(-edgeVecBuffer[i], edgeVecBuffer[(i + 1) % 3]));
                //Log.WriteLine("Triangle.IsDegenerate: angle: " + (MathF.Acos(absCosAngle) * Maths.Rad2Deg));
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
            var t = new Triangle((int)a, (int)b, (int)c)
            {
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

        public Triangle(SerializedTriangle data, Vector2[] points) : this()
        {
            var indices = data.Indices;
            Setup(indices[0], indices[1], indices[2], points);
            ClearKey();
        }


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
            GetEdges(points, edgeVecBuffer);
            GetVerts(points, vertsBuffer);
            for (int i = 0; i < 3; i++)
            {
                rayBuffer[i] = point - vertsBuffer[i];
            }
            crossBuffer[0] = Mathv.Cross(rayBuffer[2], edgeVecBuffer[2]);
            crossBuffer[1] = Mathv.Cross(rayBuffer[2], edgeVecBuffer[1]);
            crossBuffer[2] = Mathv.Cross(rayBuffer[0], edgeVecBuffer[0]);
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
            return 0.5f * MathF.Abs(Mathv.Cross(points[A] - points[C], points[B] - points[C]));
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
                return Mathv.AngleRad(points[C] - points[A], points[B] - points[A]);
            }
            else if (vertIndex == B)
            {
                return Mathv.AngleRad(points[C] - points[B], points[A] - points[B]);
            }
            else if (vertIndex == C)
            {
                return Mathv.AngleRad(points[A] - points[C], points[B] - points[C]);
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
            indexBuffer[offset++ % 3] = A;
            indexBuffer[offset++ % 3] = B;
            indexBuffer[offset++ % 3] = C;
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
            FillColor = DefaultColor;
        }

        public void SetIndices(int a, int b, int c)
        {
            A = a;
            B = b;
            C = c;
        }

        public void ComputeCircumCircle(Vector2 a, Vector2 b, Vector2 c)
        {
            float A = b.X - a.X,
                B = b.Y - a.Y,
                C = c.X - a.X,
                D = c.Y - a.Y,
                E = A * (a.X + b.X) + B * (a.Y + b.Y),
                F = C * (a.X + c.X) + D * (a.Y + c.Y),
                G = 2f * (A * (c.Y - b.Y) - B * (c.X - b.X)),
                minx, miny, dx, dy;
            float cX, cY, sqrR;

            /* If the points of the triangle are collinear, then just find the
             * extremes and use the midpoint as the center of the circumcircle. */
            if (MathF.Abs(G) < CircumCircleTolerance)
            {
                minx = MathF.Min(MathF.Min(a.X, b.X), c.X);
                miny = MathF.Min(MathF.Min(a.Y, b.Y), c.Y);
                dx = (MathF.Max(MathF.Max(a.X, b.X), c.X) - minx) * 0.5f;
                dy = (MathF.Max(MathF.Max(a.Y, b.Y), c.Y) - miny) * 0.5f;

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
                dx = cX - a.X;
                dy = cY - a.Y;
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
