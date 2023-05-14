using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;

namespace Triangulation
{
    public class EdgeFlipper
    {
        private readonly Triangle[] flipTriangles = new Triangle[2];
        private readonly int[] oppVerts = new int[2];
        private readonly int[] edgeVerts = new int[2];
        private readonly long[] triangleKeys = new long[2];
        private readonly int[] indexBuffer = new int[3];

        private readonly IncrementalEdgeInfo edgeInfo = null;
        private readonly TriangleSet triangleSet = null;
        private readonly Vector2[] points = null;
        private readonly Dictionary<int, bool> nonDelaunayEdgeDict = new Dictionary<int, bool>();
        private readonly List<int> nonDelaunayEdgeKeys = new List<int>();
        private readonly List<long> addedTriangleKeys = new List<long>();
        private readonly List<long> removedTriangleKeys = new List<long>();

        public EdgeFlipper(TriangleSet triangleSet, IncrementalEdgeInfo edgeInfo, Vector2[] points)
        {
            this.points = points;
            this.triangleSet = triangleSet;
            this.edgeInfo = edgeInfo;
        }

        public bool Validate()
        {
            bool result = edgeInfo.ForEachInnerEdge((edgeKey, edgeData) => {
                if (IsInnerEdgeNonDelaunay(edgeData))
                {
                    Log.WriteError(GetType() + ".Validate: IsInnerEdgeNonDelaunay: " + edgeKey + ", " + edgeData + Log.KIND_OF_FAKAP);
                    return false;
                }
                return true;
            });
            return result;
        }

        public void FlipEdgesFrom(Dictionary<int, EdgeEntry> edgeDict, TriangleGrid triangleGrid)
        {
            //Log.WriteWarning($"{GetType().Name}.FlipEdgesFrom");
            Clear();

            foreach (var kvp in edgeDict)
            {
                int edgeKey = kvp.Key;
                if (IsEdgeInnerNonDelaunay(edgeKey) && !nonDelaunayEdgeDict.ContainsKey(edgeKey))
                {
                    nonDelaunayEdgeKeys.Add(edgeKey);
                    nonDelaunayEdgeDict.Add(edgeKey, true);
                }
            }
            //for (int i = 0; i < nonDelaunayEdgeKeys.Count; i++)
            //{
            //    int edgeKey = nonDelaunayEdgeKeys[i];
            //    if (edgeInfo.GetInnerEdgeData(edgeKey, out var edgeData))
            //    {
            //        edgeData.Checked = false;
            //        edgeInfo.SetInnerEdgeData(edgeKey, edgeData);
            //    }
            //}
            for (int i = 0; i < nonDelaunayEdgeKeys.Count; i++)
            {
                int edgeKey = nonDelaunayEdgeKeys[i];
                if (nonDelaunayEdgeDict[edgeKey])
                {
                    FlipEdgesRecursively(edgeKey);
                }
            }
            UpdateTriangleGrid(triangleGrid);
        }

        private void Clear()
        {
            nonDelaunayEdgeKeys.Clear();
            nonDelaunayEdgeDict.Clear();
            addedTriangleKeys.Clear();
            removedTriangleKeys.Clear();
        }

        private void UpdateTriangleGrid(TriangleGrid triangleGrid)
        {
            for (int i = 0; i < removedTriangleKeys.Count; i++)
            {
                triangleGrid.RemoveTriangle(removedTriangleKeys[i]);
            }
            for (int i = 0; i < addedTriangleKeys.Count; i++)
            {
                var triangle = GetTriangleRef(addedTriangleKeys[i], out _);
                triangleGrid.AddTriangle(triangle);
            }
        }

        private void FlipEdgesRecursively(int edgeKey)
        {
            if (nonDelaunayEdgeDict.ContainsKey(edgeKey))
            {
                nonDelaunayEdgeDict[edgeKey] = false;
            }
            if (edgeInfo.GetInnerEdgeData(edgeKey, out var edgeData))
            {
                var nextEdgeKeys = FlipEdges(edgeData);

                for (int i = 0; i < nextEdgeKeys.Length; i++)
                {
                    int nextEdgeKey = nextEdgeKeys[i];

                    if (IsEdgeInnerNonDelaunay(nextEdgeKey))
                    {
                        FlipEdgesRecursively(nextEdgeKey);
                    }
                }
            }
            else
            {
                Log.WriteWarning($"{GetType().Name}.FlipEdgesRecursively: {edgeKey} NOT FOUND IN innerEdgeTriangleDict");
            }
        }

        private int[] FlipEdges(InnerEdgeData edgeData)
        {
            var nextEdgeKeys = new int[4];

            var edge = edgeData.Edge;

            edgeVerts[0] = edge.A;
            edgeVerts[1] = edge.B;
            triangleKeys[0] = edgeData.Triangle1Key;
            triangleKeys[1] = edgeData.Triangle2Key;

            //Log.WriteLine(GetType() + ".FlipEdges");
            //edgeInfo.PrintEdgeCounterDict();
            //edgeInfo.PrintExtEdgeTriangleDict();
            //edgeInfo.PrintInnerEdgeTriangleDict();

            for (int i = 0; i < 2; i++)
            {
                flipTriangles[i] = GetTriangleRef(triangleKeys[i], out int triangleIndex);
                oppVerts[i] = flipTriangles[i].GetOppositeVertex(edge);

                triangleSet.RemoveTriangle(triangleIndex, edgeInfo);

                long triangleKey = triangleKeys[i];
                removedTriangleKeys.Add(triangleKey);
                addedTriangleKeys.Remove(triangleKey);
            }
            //Log.WriteWarning($"{GetType().Name}.FlipEdges: FLIPPING TRIANGLES: {flipTriangles[0]}, {flipTriangles[1]}, flip edge: {edge}");
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    nextEdgeKeys[(i << 1) + j] = edgeInfo.GetEdgeKey(oppVerts[j], edgeVerts[i]);
                }
                flipTriangles[i] = new Triangle(oppVerts[0], oppVerts[1], edgeVerts[i], points);
                flipTriangles[i].SetKey(points.Length, indexBuffer);

                var triangle = flipTriangles[i];
                triangleSet.AddTriangle(triangle, out _);
                edgeInfo.AddEdgesToCounterDict(triangle);

                long triangleKey = triangle.Key;
                addedTriangleKeys.Add(triangleKey);
                removedTriangleKeys.Remove(triangleKey);
            }
            for (int i = 0; i < 2; i++)
            {
                edgeInfo.AddEdgesToTriangleDicts(ref flipTriangles[i], Triangle.DefaultColor);
                //if (flipTriangles[i].Key != triangleSet.Triangles[triangleSet.Count - 2 + i].Key)
                //{
                //    throw new Exception("flipTriangles[i].Key != triangleSet.Triangles[triangleSet.Count - 2 + i].Key");
                //}
                //triangleSet.Triangles[triangleSet.Count - 2 + i].FillColor = flipTriangles[i].FillColor;
            }
            //edgeInfo.PrintEdgeCounterDict();
            //edgeInfo.PrintExtEdgeTriangleDict();
            //edgeInfo.PrintInnerEdgeTriangleDict();
            //Log.WriteWarning($"{GetType()}.FlipEdges: FLIPPED TRIANGLES: {flipTriangles[0]}, {flipTriangles[1]}");

            return nextEdgeKeys;
        }

        private bool IsEdgeInnerNonDelaunay(int edgeKey/*, bool skipChecked*/)
        {
            // Debug
            {
                //var edge = edgeInfo.GetEdgeFromKey(edgeKey);
                //Log.WriteLine($"{GetType().Name}.IsEdgeInnerNonDelaunay: {edge} {edgeInfo.IsEdgeInternal(edgeKey)} {edgeInfo.GetInnerEdgeData(edgeKey, out var innerEdgeData)} {innerEdgeData}");
            }
            if (edgeInfo.IsEdgeInternal(edgeKey) && edgeInfo.GetInnerEdgeData(edgeKey, out var edgeData))
            {
                //if (skipChecked)
                //{
                //    if (edgeData.Checked)
                //    {
                //        return false;
                //    }
                //    else
                //    {
                //        edgeData.Checked = true;
                //        edgeInfo.SetInnerEdgeData(edgeKey, edgeData);
                //    }
                //}
                return IsInnerEdgeNonDelaunay(edgeData);
            }
            return false;
        }

        private bool IsInnerEdgeNonDelaunay(InnerEdgeData edgeData)
        {
            var edge = edgeData.Edge;
            var t1 = GetTriangleRef(edgeData.Triangle1Key, out _);
            var t2 = GetTriangleRef(edgeData.Triangle2Key, out _);
            float angleSum = t1.GetOppositeAngleDeg(edge, points) + t2.GetOppositeAngleDeg(edge, points);
            return angleSum > 180f;
        }

        private ref Triangle GetTriangleRef(long triangleKey, out int triangleIndex)
        {
            return ref triangleSet.GetTriangleRef(triangleKey, out triangleIndex);
        }
    }
}
