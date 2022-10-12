using System;
using System.Collections.Generic;

namespace Triangulation
{
    public class EdgeFlipper
    {
        private readonly Triangle[] flipTriangles = new Triangle[2];
        private readonly int[] oppVerts = new int[2];
        private readonly int[] edgeVerts = new int[2];
        private readonly long[] triangleKeys = new long[2];
        private readonly int[] indexBuffer = new int[3];

        private readonly EdgeInfo edgeInfo = null;
        private readonly TriangleSet triangleSet = null;
        private readonly Vector2[] points = null;
        private readonly Dictionary<int, bool> nonDelaunayEdgeDict = new Dictionary<int, bool>();
        private readonly List<int> nonDelaunayEdgeKeys = new List<int>();
        private readonly List<long> addedTriangleKeys = new List<long>();
        private readonly List<long> removedTriangleKeys = new List<long>();

        public EdgeFlipper(TriangleSet triangleSet, Vector2[] points)
        {
            this.points = points;
            this.triangleSet = triangleSet;
            edgeInfo = triangleSet.EdgeInfo;
        }

        public bool Validate()
        {
            bool result = edgeInfo.ForEachInnerEdge((edgeKey, edgeData) => {
                if (IsInnerEdgeNonDelaunay(GetEdgeFromKey(edgeKey), edgeData))
                {
                    Console.WriteLine(GetType() + ".Validate: IsInnerEdgeNonDelaunay: " + edgeKey + ", " + edgeData + Log.KIND_OF_FAKAP);
                    return false;
                }
                return true;
            });
            return result;
        }

        public void FlipEdgesFrom(Dictionary<int, EdgeEntry> edgeDict, TriangleGrid triangleGrid)
        {
            nonDelaunayEdgeKeys.Clear();
            nonDelaunayEdgeDict.Clear();
            addedTriangleKeys.Clear();
            removedTriangleKeys.Clear();

            foreach (var kvp in edgeDict)
            {
                int edgeKey = kvp.Key;
                var edge = kvp.Value;
                if (IsEdgeInnerNonDelaunay(edge, edgeKey) && !nonDelaunayEdgeDict.ContainsKey(edgeKey))
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
                    FlipEdgesRecursively(GetEdgeFromKey(edgeKey), edgeKey);
                }
            }
            for (int i = 0; i < removedTriangleKeys.Count; i++)
            {
                triangleGrid.RemoveTriangle(removedTriangleKeys[i]);
            }
            for (int i = 0; i < addedTriangleKeys.Count; i++)
            {
                var triangle = triangleSet.GetTriangleRef(addedTriangleKeys[i], out _);
                triangleGrid.AddTriangle(triangle);
            }
        }

        private void FlipEdgesRecursively(EdgeEntry edge, int edgeKey)
        {
            if (nonDelaunayEdgeDict.ContainsKey(edgeKey))
            {
                nonDelaunayEdgeDict[edgeKey] = false;
            }
            if (edgeInfo.GetInnerEdgeData(edgeKey, out var edgeData))
            {
                EdgeEntry[,] nextFlipEdges = new EdgeEntry[2, 2];

                edgeVerts[0] = edge.A;
                edgeVerts[1] = edge.B;
                triangleKeys[0] = edgeData.Triangle1Key;
                triangleKeys[1] = edgeData.Triangle2Key;

                //Console.WriteLine(GetType() + ".FlipEdgesRecursively");
                //edgeInfo.PrintEdgeCounterDict();
                //edgeInfo.PrintExtEdgeTriangleDict();
                //edgeInfo.PrintInnerEdgeTriangleDict();

                for (int i = 0; i < 2; i++)
                {
                    flipTriangles[i] = triangleSet.GetTriangleRef(triangleKeys[i], out int triangleIndex);
                    oppVerts[i] = flipTriangles[i].GetOppositeVertex(edge);

                    triangleSet.RemoveTriangle(triangleIndex);

                    long triangleKey = triangleKeys[i];
                    removedTriangleKeys.Add(triangleKey);
                    addedTriangleKeys.Remove(triangleKey);
                }
                Console.WriteLine(GetType() + ".FlipEdgesRecursively: FLIPPING TRIANGLES: " + flipTriangles[0] + ", " + flipTriangles[1] + ", flip edge: " + edge);
                for (int i = 0; i < 2; i++)
                {
                    for (int j = 0; j < 2; j++)
                    {
                        nextFlipEdges[i, j] = new EdgeEntry(oppVerts[j], edgeVerts[i]);
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
                    edgeInfo.AddEdgesToTriangleDicts(ref flipTriangles[i], Color.FloralWhite);
                    //if (flipTriangles[i].Key != triangleSet.Triangles[triangleSet.Count - 2 + i].Key)
                    //{
                    //    throw new Exception("flipTriangles[i].Key != triangleSet.Triangles[triangleSet.Count - 2 + i].Key");
                    //}
                    //triangleSet.Triangles[triangleSet.Count - 2 + i].FillColor = flipTriangles[i].FillColor;
                }
                //edgeInfo.PrintEdgeCounterDict();
                //edgeInfo.PrintExtEdgeTriangleDict();
                //edgeInfo.PrintInnerEdgeTriangleDict();
                Console.WriteLine(GetType() + ".FlipEdgesRecursively: FLIPPED TRIANGLES: " + flipTriangles[0] + ", " + flipTriangles[1]);

                for (int i = 0; i < 2; i++)
                {
                    for (int j = 0; j < 2; j++)
                    {
                        edge = nextFlipEdges[i, j];
                        edgeKey = GetEdgeKey(edge);
                        if (IsEdgeInnerNonDelaunay(edge, edgeKey))
                        {
                            FlipEdgesRecursively(edge, edgeKey);
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine(GetType() + ".FlipEdgesRecursively: " + edge + " NOT FOUND IN innerEdgeTriangleDict");
            }
        }

        private bool IsEdgeInnerNonDelaunay(EdgeEntry edge, int edgeKey/*, bool skipChecked*/)
        {
            //Console.WriteLine(GetType() + ".IsEdgeInnerNonDelaunay: " + edge + " " + edgeInfo.IsEdgeInternal(edgeKey) + " " + edgeInfo.GetInnerEdgeData(edgeKey, out var edgeData) + " " + edgeData);
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
                return IsInnerEdgeNonDelaunay(edge, edgeData);
            }
            return false;
        }

        private bool IsInnerEdgeNonDelaunay(EdgeEntry edge, InnerEdgeData edgeData)
        {
            var t1 = triangleSet.GetTriangleRef(edgeData.Triangle1Key, out _);
            var t2 = triangleSet.GetTriangleRef(edgeData.Triangle2Key, out _);
            float angleSum = t1.GetOppositeAngleDeg(edge, points) + t2.GetOppositeAngleDeg(edge, points);
            return angleSum > 180f;
        }

        private EdgeEntry GetEdgeFromKey(int key)
        {
            return edgeInfo.GetEdgeFromKey(key);
        }

        private int GetEdgeKey(EdgeEntry edge)
        {
            return edge.A * points.Length + edge.B;
        }
    }
}
