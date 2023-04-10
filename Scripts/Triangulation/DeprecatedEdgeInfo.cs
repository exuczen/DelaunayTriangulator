using System;
using System.Drawing;

namespace Triangulation
{
    public class DeprecatedEdgeInfo : EdgeInfo
    {
        public DeprecatedEdgeInfo(TriangleSet triangleSet, Vector2[] points, IExceptionThrower exceptionThrower) : base(triangleSet, points, exceptionThrower)
        {
        }

        public void ClearCheckedInnerEdges()
        {
            edgeIndexPool.Clear();
            edgeIndexPool.AddRange(innerEdgeTriangleDict.Keys);
            for (int i = 0; i < edgeIndexPool.Count; i++)
            {
                int edgeKey = edgeIndexPool[i];
                var edgeData = innerEdgeTriangleDict[edgeKey];
                edgeData.Checked = false;
                innerEdgeTriangleDict[edgeKey] = edgeData;
            }
        }

        //public bool JoinLastTwoExternalEdges(int extPointIndex, out EdgePeak extPeak)
        //{
        //    if (extEdgeCount < 4)
        //    {
        //        Log.WriteLine(GetType() + ".JoinLastTwoExtEdges: extEdgeCount: " + extEdgeCount);
        //        extPeak = default;
        //        return false;
        //    }
        //    var edgeY = extEdges[extEdgeCount - 2];
        //    var edgeZ = extEdges[extEdgeCount - 1];
        //    extPeak = new EdgePeak(edgeY, edgeZ, points);
        //    int sharedVertex = EdgeEntry.GetSharedVertex(edgeY, edgeZ);
        //    if (sharedVertex != extPointIndex)
        //    {
        //        throw new Exception("JoinLastTwoExtEdges: " + sharedVertex + " != " + extPointIndex);
        //    }
        //    int a = edgeY.GetOtherVertex(extPointIndex);
        //    int b = edgeZ.GetOtherVertex(extPointIndex);
        //    extEdges[extEdgeCount - 2] = new EdgeEntry(a, b);
        //    extEdges[extEdgeCount - 1] = default;
        //    extEdgeCount--;
        //    return true;
        //}

        //public bool ContainsEdge(EdgeEntry edge, out int edgeKey)
        //{
        //    edgeKey = GetEdgeKey(edge);
        //    return edgeCounterDict.ContainsKey(edgeKey);
        //}

        //public void ForEachInnerEdge(Action<int, InnerEdgeData> action)
        //{
        //    foreach (var kvp in innerEdgeTriangleDict)
        //    {
        //        action(kvp.Key, kvp.Value);
        //    }
        //}

        //public void InvokeForExternalEdgesRange(IndexRange range, Func<EdgeEntry, EdgeEntry> action)
        //{
        //    int edgeIndex = range.Beg;
        //    while (edgeIndex != range.End)
        //    {
        //        extEdges[edgeIndex] = action(extEdges[edgeIndex]);
        //        edgeIndex = extEdges[edgeIndex].Next;
        //    }
        //    extEdges[range.End] = action(extEdges[range.End]);
        //}

        //public int GetInternalEdgeCount(Triangle triangle)
        //{
        //    int count = 0;
        //    ForEachInternalEdge(triangle, (edgeKey, edge) => count++);
        //    return count;
        //}

        //public int GetExternalEdges(Triangle triangle, EdgeEntry[] edges)
        //{
        //    int count = 0;
        //    ForEachExternalEdge(triangle, (edgeKey, edge) => {
        //        edges[count++] = edge;
        //    });
        //    return count;
        //}
    }
}
