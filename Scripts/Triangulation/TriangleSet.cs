using System;
using System.Collections.Generic;

namespace Triangulation
{
    public class TriangleSet
    {
        public int PointsLength => pointsLength;
        public int Capacity => triangles.Length;
        public int Count => trianglesDict.Count;
        public Triangle[] Triangles => triangles;
        public EdgeInfo EdgeInfo => edgeInfo;
        public EdgeFlipper EdgeFlipper => edgeFlipper;

        private readonly Dictionary<long, int> trianglesDict = new Dictionary<long, int>();
        private readonly Triangle[] triangles = null;
        private readonly EdgeInfo edgeInfo = null;
        private readonly EdgeFlipper edgeFlipper = null;

        private readonly int pointsLength = 0;
        private readonly int[] indexBuffer = new int[3];

        public TriangleSet(Triangle[] triangles, Vector2[] points)
        {
            this.triangles = triangles;
            edgeInfo = new EdgeInfo(triangles.Length << 1, this, points);
            edgeFlipper = new EdgeFlipper(this, points);
            pointsLength = points.Length;
        }

        public TriangleSet(int trianglesCapacity, Vector2[] points) : this(new Triangle[trianglesCapacity], points)
        {
        }

        public void Clear()
        {
            trianglesDict.Clear();
            edgeInfo.Clear();
        }

        public bool ContainsTriangle(long triangleKey) => trianglesDict.ContainsKey(triangleKey);

        public ref Triangle GetTriangleRef(long triangleKey, out int triangleIndex)
        {
            triangleIndex = trianglesDict[triangleKey];
            return ref triangles[triangleIndex];
        }

        //public void SetTriangles(Triangle[] triangles, int trianglesCount, bool setTriangleKeys)
        //{
        //    if (this.triangles != triangles)
        //    {
        //        throw new Exception("this.triangles != triangles");
        //    }
        //    Clear();
        //
        //    if (setTriangleKeys)
        //    {
        //        for (int i = 0; i < trianglesCount; i++)
        //        {
        //            triangles[i].SetKey(pointsLength, indexBuffer);
        //            trianglesDict.Add(triangles[i].Key, i);
        //        }
        //    }
        //    else
        //    {
        //        for (int i = 0; i < trianglesCount; i++)
        //        {
        //            trianglesDict.Add(triangles[i].Key, i);
        //        }
        //    }
        //}

        //public int AddTriangles(TriangleSet triangleSet, Color innerColor)
        //{
        //    var addedTriangles = triangleSet.Triangles;
        //    int addedCount = triangleSet.Count;
        //
        //    AddTriangles(addedTriangles, addedCount, innerColor, false);
        //
        //    return Count;
        //}

        public int AddTriangles(Triangle[] addedTriangles, int addedCount, Color innerColor)
        {
            if (addedTriangles == triangles)
            {
                for (int i = 0; i < addedCount; i++)
                {
                    addedTriangles[i].SetKey(pointsLength, indexBuffer);
                    trianglesDict.Add(addedTriangles[i].Key, i);
                }
            }
            else
            {
                for (int i = 0; i < addedCount; i++)
                {
                    addedTriangles[i].SetKey(pointsLength, indexBuffer);
                    AddTriangle(addedTriangles[i], out _);
                }
            }
            AddEdgesToDicts(addedTriangles, addedCount, innerColor);

            return Count;
        }

        public void AddTriangle(Triangle triangle, out int triangleIndex)
        {
            trianglesDict.Add(triangle.Key, triangleIndex = Count);
            triangles[triangleIndex] = triangle;
        }

        public bool TryAddTriangle(Triangle triangle, out int triangleIndex)
        {
            if (ContainsTriangle(triangle.Key))
            {
                triangleIndex = -1;
            }
            else
            {
                AddTriangle(triangle, out triangleIndex);
            }
            return triangleIndex >= 0;
        }

        public void RemoveTriangleWithKey(long triangleKey, ref int trianglesCount)
        {
            if (!ContainsTriangle(triangleKey))
            {
                throw new Exception("RemoveTriangle: no triangle key in trianglesDict: " + triangleKey);
            }
            RemoveTriangle(trianglesDict[triangleKey], ref trianglesCount);
        }

        public void RemoveTriangle(int triangleIndex, ref int trianglesCount)
        {
            RemoveTriangle(triangleIndex);
            trianglesCount = Count;
        }

        public void RemoveTriangle(int triangleIndex)
        {
            int trianglesCount = Count;
            if (triangleIndex >= trianglesCount || trianglesCount <= 0 || triangleIndex < 0)
            {
                throw new Exception("RemoveTriangle: trianglesCount: " + trianglesCount + " triangleIndex: " + triangleIndex);
            }
            ref var triangle = ref triangles[triangleIndex];
            //Log.WriteLine(GetType() + ".RemoveTriangle: " + triangle);
            triangle.FillColor = Color.Black;

            edgeInfo.RemoveEdgesFromDicts(triangle);
            trianglesDict.Remove(triangle.Key);

            triangles[triangleIndex] = triangles[--trianglesCount];
            triangles[trianglesCount].ClearKey();

            if (triangleIndex < trianglesCount)
            {
                long triangleKey = triangles[triangleIndex].Key;
                if (!trianglesDict.ContainsKey(triangleKey))
                {
                    throw new Exception("RemoveTriangle: !trianglesDict.ContainsKey(triangleKey): " + triangleKey);
                }
                trianglesDict[triangleKey] = triangleIndex;
            }
            if (trianglesCount != Count)
            {
                throw new Exception(string.Format("{0} != {1}", trianglesCount, Count));
            }
        }

        public void FlipEdgesFrom(Dictionary<int, EdgeEntry> edgeDict, TriangleGrid triangleGrid)
        {
            edgeFlipper.FlipEdgesFrom(edgeDict, triangleGrid);
        }

        private void AddEdgesToDicts(Triangle[] triangles, int trianglesCount, Color innerColor)
        {
            edgeInfo.AddEdgesToCounterDict(triangles, trianglesCount);
            edgeInfo.AddEdgesToTriangleDicts(triangles, trianglesCount, innerColor);

            //edgeInfo.PrintEdgeCounterDict();
            //edgeInfo.PrintExtEdgeTriangleDict();
            //edgeInfo.PrintInnerEdgeTriangleDict();
        }
    }
}
