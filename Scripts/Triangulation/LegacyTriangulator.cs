// Delaunay 2D triangulation algorithm.
// Salvatore Previti. http://www.salvatorepreviti.it - info@salvatorepreviti.it
// Optimized implementation of Delaunay triangulation algorithm by Paul Bourke (pbourke@swin.edu.au)
// See http://astronomy.swin.edu.au/~pbourke/terrain/triangulate/ for details.
//
// You can use this code or parts of this code providing that above credit remain intact.
//

using System;
using System.Collections.Generic;
using System.Numerics;

namespace Triangulation
{
    //public struct IntegerTriangle
    //{
    //    public int A;
    //    public int B;
    //    public int C;
    //}

    /// <summary>
    /// A specialized very fast table of edges for triangulation.
    /// </summary>
    public class LegacyTriangulator
    {
        #region Public properties

        public int PointsCount => pointsCount;
        public int TrianglesCount => trianglesCount;
        public Vector2[] Points => points;
        public Triangle[] Triangles => triangles;
        public Bounds2 Bounds => bounds;

        #endregion

        #region Points fields

        protected int pointsCount;
        protected readonly float tolerance;
        protected readonly Vector2[] points;
        protected Bounds2 bounds;

        #endregion

        #region Triangles Fields

        protected readonly Triangle[] triangles;

        protected int trianglesLast;
        protected int trianglesCount;
        protected int trianglesFirst;

        protected int firstNonCompletedTriangle;
        protected int lastNonCompletedTriangle;

        protected int firstFreeTriangle;

        #endregion

        #region Edges Fields

        protected readonly EdgeBucketEntry[] edgesBuckets;
        protected readonly EdgeEntry[] edgesEntries;

        protected int edgesBucketsCount;
        protected int edgesGeneration;
        protected int edgesCount;

        #endregion

        #region Triangulate

        public void Triangulate(ICollection<Vector2> pointsCollection)
        {
            if (Initialize(pointsCollection))
            {
                Process();
            }
        }

        //public void Triangulate(ICollection<Vector2> pointsCollection, IList<IntegerTriangle> destination)
        //{
        //    if (Initialize(pointsCollection))
        //    {
        //        Process();
        //        AddTo(destination);
        //    }
        //}

        #endregion

        #region Initialize

        public LegacyTriangulator(int pointsCapacity, float tolerance)
        {
            this.tolerance = tolerance > 0f ? tolerance : Mathv.Epsilon; // Ensure tolerance is valid

            // Create the array of points.
            // We need 3 more items to add supertriangle vertices
            points = new Vector2[pointsCapacity + 3];

            // Create triangle array
            int trianglesCapacity = pointsCapacity * 4 + 1;
            triangles = new Triangle[trianglesCapacity];

            // Create edge table
            int edgeEntrySize = Maths.GetPrime(trianglesCapacity * 3 + 1);
            edgesBuckets = new EdgeBucketEntry[edgeEntrySize];
            edgesEntries = new EdgeEntry[edgeEntrySize];
        }

        public bool Initialize(ICollection<Vector2> pointsCollection)
        {
            ClearIndexData();

            int pointsCountPrev = pointsCount;
            pointsCount = pointsCollection.Count;
            trianglesCount = 0;

            pointsCollection.CopyTo(points, 0);

            if (pointsCount < 3)
            {
                bounds = default;
                return false; // We need a non null collection with at least 3 vertices!
            }

            // Calculate min and max X and Y coomponents of points

            bounds = Bounds2.GetBounds(points, 0, pointsCount - 1);

            // Initialized free triangles

            int trianglesCountPrev = pointsCountPrev > 0 ? pointsCountPrev * 4 + 1 : 0;
            trianglesCount = pointsCount * 4 + 1;
            int maxTrianglesCount = Math.Max(trianglesCountPrev, trianglesCount);

            Triangle triangleEntry = new Triangle
            {
                PrevNonCompleted = -1,
                NextNonCompleted = -1
            };
            for (int i = 0; i < trianglesCount - 1; ++i)
            {
                triangleEntry.Previous = i - 1;
                triangleEntry.Next = i + 1;

                triangles[i] = triangleEntry;
            }
            for (int i = trianglesCount - 1; i < maxTrianglesCount; ++i)
            {
                triangleEntry.Previous = i - 1;
                triangleEntry.Next = -1;

                triangles[i] = triangleEntry;
            }

            // Initialize edge table

            for (int i = 0; i < edgesBucketsCount; i++)
            {
                edgesBuckets[i] = default;
                edgesEntries[i] = default;
            }
            edgesBucketsCount = Maths.GetPrime(trianglesCount * 3 + 1);

            return true;
        }

        protected void ClearIndexData()
        {
            // Initialize triangle table

            trianglesFirst = -1;
            trianglesLast = -1;

            firstNonCompletedTriangle = -1;
            lastNonCompletedTriangle = -1;

            firstFreeTriangle = 0;

            // Initialize edge table

            edgesGeneration = 1;
            edgesCount = 0;
        }

        #endregion

        #region Process

        private void Process()
        {
            // Sort points

            IComparer<Vector2> pointsComparer;
            Vector2 boundsSize = bounds.Size;
            bool xySorted = boundsSize.X > boundsSize.Y;
            if (xySorted)
            {
                // Sort points by X (firstly), Y (secondly)
                pointsComparer = new PointsXYComparer(tolerance);
            }
            else // yxSorted
            {
                // Sort points by Y (firstly), X (secondly)
                pointsComparer = new PointsYXComparer(tolerance);
            }
            Array.Sort(points, 0, pointsCount, pointsComparer);

            // Add supertriangle

            AddSuperTriangle(bounds, pointsCount);

            // Process all sorted points

            Circle circumCircle;
            Vector2 point = pointsCount > 0 ? points[0] : default;
            Vector2 prevPoint;
            Vector2 dr;

            for (int pointIndex = 0; pointIndex < pointsCount; ++pointIndex)
            {
                prevPoint = point;
                point = points[pointIndex];

                //Log.WriteLine(GetType() + "." + points[pointIndex] + " " + pointsYX[sortedIndex]);

                if (pointIndex != 0 && MathF.Abs(point.X - prevPoint.X) < tolerance && MathF.Abs(point.Y - prevPoint.Y) < tolerance)
                {
                    continue; // Ignore current point if equals to previous point. We check equality using tolerance.
                }

                // Check if triangle contains current point in its circumcenter.
                // If yes, add triangle edges to edges table and remove triangle.
                for (int nextNonCompleted, triangleIndex = firstNonCompletedTriangle; triangleIndex >= 0; triangleIndex = nextNonCompleted)
                {
                    // Calculate distance between triancle circumcircle center and current point
                    // Compare that distance with radius of triangle circumcircle
                    // If is less, it means that the point is inside of circumcircle, else, it means it is outside.

                    circumCircle = triangles[triangleIndex].CircumCircle;
                    nextNonCompleted = triangles[triangleIndex].NextNonCompleted;

                    dr = point - circumCircle.Center;
                    float sqrDx = dr.X * dr.X;
                    float sqrDy = dr.Y * dr.Y;

                    if (sqrDx + sqrDy <= circumCircle.SqrRadius)
                    {
                        // Point is inside triangle circumcircle.
                        // Add triangle edges to edge table and remove the triangle

                        ReplaceTriangleWithEdges(triangleIndex, ref triangles[triangleIndex]);
                    }
                    else
                    {
                        bool completed;
                        if (xySorted)
                        {
                            completed = (dr.X > -tolerance) && (sqrDx > circumCircle.SqrRadius + tolerance);
                        }
                        else // yxSorted
                        {
                            completed = (dr.Y > -tolerance) && (sqrDy > circumCircle.SqrRadius + tolerance);
                        }
                        if (completed)
                        {
                            // Triangle not need to be checked anymore.
                            // Remove it from linked list of non completed triangles.

                            MarkAsComplete(ref triangles[triangleIndex]);
                        }
                    }
                }

                ReplaceEdgesWithTriangles(pointIndex);
            }

            firstNonCompletedTriangle = -1;

            // Find valid triangles (triangles that don't share vertices with supertriangle) and find the last triangle.

            FindValidTriangles();
        }

        #endregion

        #region ToIntegerTriangle

        //private void AddTo(IList<IntegerTriangle> list)
        //{
        //    if (list is List<IntegerTriangle> llist)
        //    {
        //        if (llist.Capacity < llist.Count + trianglesCount)
        //        {
        //            llist.Capacity = llist.Count + trianglesCount + 4;
        //        }
        //    }

        //    ForEachIntegerTriangle((triangle, counter) => list.Add(triangle));
        //}

        //private IntegerTriangle[] ToIntegerTriangles()
        //{
        //    IntegerTriangle[] array = new IntegerTriangle[trianglesCount];
        //
        //    ForEachIntegerTriangle((triangle, counter) => array[counter] = triangle);
        //
        //    return array;
        //}

        //private void ForEachIntegerTriangle(Action<IntegerTriangle, int> action)
        //{
        //    int counter = 0;
        //    for (int triangleIndex = trianglesLast; triangleIndex >= 0; triangleIndex = triangles[triangleIndex].Previous)
        //    {
        //        if (triangles[triangleIndex].A >= 0)
        //        {
        //            action(triangles[triangleIndex].ToIntegerTriangle(), counter++);
        //            //Log.WriteLine(GetType() + ".IntegerTriangles: " + triangleIndex + " : " + triangles[triangleIndex]);
        //        }
        //    }
        //}

        #endregion


        protected void FindValidTriangles()
        {
            Dictionary<int, int> indicesDict = new Dictionary<int, int>();
            HashSet<Triangle> validTriangles = new HashSet<Triangle>();

            int triangleIndex = trianglesLast = trianglesFirst;

            while (triangleIndex >= 0)
            {
                Triangle triangle = triangles[trianglesLast = triangleIndex];

                if (triangle.A < pointsCount && triangle.B < pointsCount && triangle.C < pointsCount)
                {
                    // Valid triangle found. Increment count.
                    indicesDict.Add(triangleIndex, validTriangles.Count);
                    //Log.WriteLine(GetType() + ".FindValidTriangles: " + triangleIndex + " -> " + validTriangles.Count + " : " + triangle);
                    validTriangles.Add(triangle);
                }
                else
                {
                    // Current triangle is invalid. Mark it as invalid
                    triangles[triangleIndex].A = -1;
                }

                triangleIndex = triangles[triangleIndex].Next;
            }
            //Log.WriteLine("FindValidTriangles: {0}, {1}, {2}", trianglesFirst, trianglesLast, validTriangles.Count);
            //ToIntegerTriangles();

            int validTrianglesCount = validTriangles.Count;
            triangleIndex = validTrianglesCount - 1;

            foreach (var validTriangle in validTriangles)
            {
                triangles[triangleIndex] = validTriangle;
                ref var triangle = ref triangles[triangleIndex];
                triangle.Next = indicesDict.TryGetValue(triangle.Next, out int next) ? validTrianglesCount - 1 - next : -1;
                triangle.Previous = indicesDict.TryGetValue(triangle.Previous, out int prev) ? validTrianglesCount - 1 - prev : -1;
                triangle.ClearNotCompleted();

                triangleIndex--;
            }

            //for (int i = 0; i < validTrianglesCount; i++)
            //{
            //    Log.WriteLine(GetType() + ".ValidTriangles: " + i + " : " + triangles[i]);
            //}

            for (int i = validTrianglesCount; i < trianglesCount; i++)
            {
                triangles[i].ClearPrevNext();
            }

            trianglesCount = validTrianglesCount;
        }

        #region Edges table

        protected int ReplaceEdgesWithTriangles(int pointIndex)
        {
            // Form new triangles for the current point
            // Edges used more than once will be skipped
            // Triangle vertices are arranged in clockwise order

            int count = 0;
            for (int j = 0; j < edgesCount; ++j)
            {
                EdgeEntry edge = edgesEntries[j];
                if (edgesEntries[j].Count == 1)
                {
                    // If edge was used only one time, add a new triangle built from current edge.

                    //var t =
                    AddTriangle(edge.A, edge.B, pointIndex);

                    //Log.WriteLine(GetType() + ".ReplaceEdgesWithTriangles: " + t);

                    count++;
                }
            }

            // Clear edges table

            ++edgesGeneration;
            edgesCount = 0;

            return count;
        }

        protected void ReplaceTriangleWithEdges(int triangleIndex, ref Triangle triangle)
        {
            // Remove triangle from linked list

            if (triangle.Next >= 0)
            {
                triangles[triangle.Next].Previous = triangle.Previous;
            }

            if (triangle.Previous >= 0)
            {
                triangles[triangle.Previous].Next = triangle.Next;
            }
            else
            {
                trianglesFirst = triangle.Next;
                //Log.WriteLine("ReplaceTriangleWithEdges: {0}, {1}", triangleIndex, trianglesFirst);
            }

            // Remove triangle from non completed linked list

            MarkAsComplete(ref triangle);

            // Add triangle to free triangles linked list

            triangle.Previous = -1;
            triangle.Next = firstFreeTriangle;
            triangles[firstFreeTriangle].Previous = triangleIndex;
            firstFreeTriangle = triangleIndex;

            //Log.WriteLine(GetType() + ".ReplaceTriangleWithEdges: " + triangleIndex + " : " + triangle);

            // Add triangle edges to edges table

            AddEdge(triangle.A, triangle.B);
            AddEdge(triangle.B, triangle.C);
            AddEdge(triangle.C, triangle.A);
        }

        private void AddEdge(int edgeA, int edgeB)
        {
            EdgeEntry entry = new EdgeEntry
            {
                Prev = -1
            };
            // Calculate bucked index using an hashcode of edge indices.
            // Hashcode is generated so order of edges is ignored, it means, edge 1, 2 is equals to edge 2, 1
            int targetBucket = unchecked(((edgeA < edgeB ? (edgeA << 8) ^ edgeB : (edgeB << 8) ^ edgeA) & 0x7FFFFFFF) % edgesBucketsCount);

            if (edgesBuckets[targetBucket].generation != edgesGeneration)
            {
                // Bucket generation doesn't match current generation.
                // This means this bucket is empty.
                // Generations are incremented each time edge table is cleared.

                // This entry is in the head of this bucket
                entry.Next = -1;

                // Store the new generation
                edgesBuckets[targetBucket].generation = edgesGeneration;
            }
            else
            {
                int entryIndex = edgesBuckets[targetBucket].entryIndex;

                for (int i = entryIndex; i >= 0; i = entry.Next)
                {
                    entry = edgesEntries[i];
                    if ((entry.A == edgeA && entry.B == edgeB) || (entry.A == edgeB && entry.B == edgeA))
                    {
                        ++edgesEntries[i].Count;
                        return;
                    }
                }

                entry.Next = entryIndex;
            }

            entry.A = edgeA;
            entry.B = edgeB;
            entry.Count = 1;

            edgesEntries[edgesCount] = entry;
            edgesBuckets[targetBucket].entryIndex = edgesCount;
            ++edgesCount;
        }

        #endregion

        #region Triangles lists

        protected void AddSuperTriangle(Bounds2 bounds, int pointsCount)
        {
            Vector2 d = bounds.max - bounds.min;
            float dmax = (d.X > d.Y) ? d.X : d.Y;
            Vector2 mid = (bounds.max + bounds.min) * 0.5f;

            // Create supertriangle vertices
            points[pointsCount] = new Vector2(mid.X - 2 * dmax, mid.Y - dmax);
            points[pointsCount + 1] = new Vector2(mid.X, mid.Y + 2 * dmax);
            points[pointsCount + 2] = new Vector2(mid.X + 2 * dmax, mid.Y - dmax);

            AddTriangle(pointsCount, pointsCount + 1, pointsCount + 2);
        }

        private ref Triangle AddTriangle(int a, int b, int c)
        {
            // Acquire the first free triangle

            int triangleIndex = firstFreeTriangle;
            firstFreeTriangle = triangles[triangleIndex].Next;
            triangles[firstFreeTriangle].Previous = -1;

            // Insert the triangle into triangles linked list

            Triangle triangle = new Triangle
            {
                Previous = -1,
                Next = trianglesFirst
            };

            if (trianglesFirst != -1)
            {
                triangles[trianglesFirst].Previous = triangleIndex;
            }

            trianglesFirst = triangleIndex;

            //Log.WriteLine("AddTriangle: {0}, {1}, {2}", trianglesFirst, triangle.Next, firstFreeTriangle);

            // Insert the triangle into non completed triangles linked list

            triangle.PrevNonCompleted = lastNonCompletedTriangle;
            triangle.NextNonCompleted = -1;

            if (firstNonCompletedTriangle == -1)
            {
                firstNonCompletedTriangle = triangleIndex;
            }
            else
            {
                triangles[lastNonCompletedTriangle].NextNonCompleted = triangleIndex;
            }

            lastNonCompletedTriangle = triangleIndex;

            // Setup the new triangle

            triangle.Setup(a, b, c, points);

            // Store new entry
            // Store the new triangle

            triangles[triangleIndex] = triangle;

            //Log.WriteLine("AddTriangle: {0}, {1}", triangleIndex, triangle);

            return ref triangles[triangleIndex];
        }

        protected void MarkAsComplete(ref Triangle triangle)
        {
            // Remove triangle from non completed linked list

            if (triangle.NextNonCompleted >= 0)
            {
                triangles[triangle.NextNonCompleted].PrevNonCompleted = triangle.PrevNonCompleted;
            }
            else
            {
                lastNonCompletedTriangle = triangle.PrevNonCompleted;
            }

            if (triangle.PrevNonCompleted >= 0)
            {
                triangles[triangle.PrevNonCompleted].NextNonCompleted = triangle.NextNonCompleted;
            }
            else
            {
                firstNonCompletedTriangle = triangle.NextNonCompleted;
            }
        }

        #endregion

    }
}
