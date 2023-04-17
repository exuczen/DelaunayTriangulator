using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;

namespace Triangulation
{
    public class TriangleSet
    {
        public int PointsLength => pointsLength;
        public int Capacity => triangles.Length;
        public int Count => trianglesDict.Count;
        public Triangle[] Triangles => triangles;

        private readonly Dictionary<long, int> trianglesDict = new Dictionary<long, int>();
        private readonly Triangle[] triangles = null;

        private readonly int pointsLength = 0;
        private readonly int[] indexBuffer = new int[3];

        public TriangleSet(Triangle[] triangles, Vector2[] points)
        {
            this.triangles = triangles;
            pointsLength = points.Length;
        }

        public void Clear()
        {
            trianglesDict.Clear();
        }

        public bool ContainsTriangle(long triangleKey) => trianglesDict.ContainsKey(triangleKey);

        public ref Triangle GetTriangleRef(long triangleKey, out int triangleIndex)
        {
            triangleIndex = trianglesDict[triangleKey];
            return ref triangles[triangleIndex];
        }

        public int AddTriangles(Triangle[] addedTriangles, int addedCount)
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

        public void RemoveTriangleWithKey(long triangleKey, ref int trianglesCount, EdgeInfo edgeInfo)
        {
            if (!ContainsTriangle(triangleKey))
            {
                throw new Exception("RemoveTriangle: no triangle key in trianglesDict: " + triangleKey);
            }
            RemoveTriangle(trianglesDict[triangleKey], ref trianglesCount, edgeInfo);
        }

        public void RemoveTriangle(int triangleIndex, ref int trianglesCount, EdgeInfo edgeInfo)
        {
            RemoveTriangle(triangleIndex, edgeInfo);
            trianglesCount = Count;
        }

        public void RemoveTriangle(int triangleIndex, EdgeInfo edgeInfo)
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
    }
}
