using System;
using System.Drawing;

namespace Triangulation
{
    public class DeprecatedEdgeInfo : EdgeInfo
    {
        public DeprecatedEdgeInfo(TriangleSet triangleSet, Vector2[] points, IExceptionThrower exceptionThrower) : base(triangleSet, points, exceptionThrower)
        {
        }

        public void ReplaceExternalEdges(EdgeEntry[] addedExtEdges, IndexRange addedRange)
        {
            var addedInvRange = addedRange.GetInverseRange();
            var removedRange = GetBaseExternalEdgesRange(addedExtEdges, addedInvRange);
            if (removedRange.GetIndexCount() < extEdgeCount)
            {
                int addedCount;
                if (addedRange.Beg > 0)
                {
                    addedCount = ArrayUtils.CutRange(addedExtEdges, addedInvRange, addedExtEdges);
                }
                else
                {
                    addedCount = addedRange.GetIndexCount();
                }
                Log.WriteLine(GetType() + ".ReplaceExternalEdges: addedRange: " + addedRange + " addedInverseRange: " + addedInvRange + " removedRange: " + removedRange);
                //Log.PrintEdges(addedExtEdges, addedCount, "ReplaceExternalEdges: PrintAddedExternalEdges: ");

                //PrintExternalEdges("ReplacePeakExternalEdges 1");
                InsertExternalEdges(addedExtEdges, addedCount, removedRange, true);
                //PrintExternalEdges("ReplacePeakExternalEdges 2");
            }
            else
            {
                extEdgeCount = 0;
            }
        }

        public void InvokeForEdgesWithPredicate(Triangle triangle, Func<EdgeEntry, int, bool> predicate, Action<int> action)
        {
            triangle.GetEdges(edgeBuffer);
            for (int i = 0; i < 3; i++)
            {
                int edgeKey = GetEdgeKey(edgeBuffer[i]);
                if (predicate(edgeBuffer[i], edgeKey))
                {
                    action(edgeKey);
                }
            }
        }

        public bool JoinLastTwoExternalEdges(int extPointIndex, out EdgePeak extPeak)
        {
            if (extEdgeCount < 4)
            {
                Log.WriteLine(GetType() + ".JoinLastTwoExtEdges: extEdgeCount: " + extEdgeCount);
                extPeak = default;
                return false;
            }
            var edgeY = extEdges[extEdgeCount - 2];
            var edgeZ = extEdges[extEdgeCount - 1];
            extPeak = new EdgePeak(edgeY, edgeZ, points);
            int sharedVertex = EdgeEntry.GetSharedVertex(edgeY, edgeZ);
            if (sharedVertex != extPointIndex)
            {
                throw new Exception("JoinLastTwoExtEdges: " + sharedVertex + " != " + extPointIndex);
            }
            int a = edgeY.GetOtherVertex(extPointIndex);
            int b = edgeZ.GetOtherVertex(extPointIndex);
            extEdges[extEdgeCount - 2] = new EdgeEntry(a, b);
            extEdges[extEdgeCount - 1] = default;
            extEdgeCount--;
            return true;
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

        public bool ContainsEdge(EdgeEntry edge, out int edgeKey)
        {
            edgeKey = GetEdgeKey(edge);
            return edgeCounterDict.ContainsKey(edgeKey);
        }

        public void ForEachInnerEdge(Action<int, InnerEdgeData> action)
        {
            foreach (var kvp in innerEdgeTriangleDict)
            {
                action(kvp.Key, kvp.Value);
            }
        }

        public void InvokeForExternalEdgesRange(IndexRange range, Func<EdgeEntry, EdgeEntry> action)
        {
            int edgeIndex = range.Beg;
            while (edgeIndex != range.End)
            {
                extEdges[edgeIndex] = action(extEdges[edgeIndex]);
                edgeIndex = extEdges[edgeIndex].Next;
            }
            extEdges[range.End] = action(extEdges[range.End]);
        }

        public int GetInternalEdgeCount(Triangle triangle)
        {
            int count = 0;
            ForEachInternalEdge(triangle, (edgeKey, edge) => count++);
            return count;
        }

        public int GetExternalEdges(Triangle triangle, EdgeEntry[] edges)
        {
            int count = 0;
            ForEachExternalEdge(triangle, (edgeKey, edge) => {
                edges[count++] = edge;
            });
            return count;
        }

        private int GetNextEdgeSharedVertex(int extEdgeIndex, bool forward, out int nextEdgeIndex)
        {
            var edge = extEdges[extEdgeIndex];
            nextEdgeIndex = forward ? edge.Next : edge.Prev;
            var nextEdge = extEdges[nextEdgeIndex];
            return EdgeEntry.GetSharedVertex(edge, nextEdge);
        }

        private bool GetFirstExternalEdge(TriangleCell cell, out int edgeIndex, out Triangle triangle)
        {
            var triangleKeys = cell.TriangleKeys;

            foreach (long tKey in triangleKeys)
            {
                ref var triangleRef = ref GetTriangleRef(tKey, out _);
                if (GetFirstExternalEdge(triangleRef, out var edge))
                {
                    edgeIndex = GetExternalEdgeIndex(edge);
                    if (edgeIndex >= 0)
                    {
                        Log.WriteLine(GetType() + ".GetFirstExternalEdge: " + edge + " " + triangleRef);
                        triangle = triangleRef;
                        return true;
                    }
                }
            }
            triangle = Triangle.None;
            edgeIndex = -1;
            return false;
        }

        private void SwapExternalEdges(int i, int j, EdgeEntry[] extEdges, int extEdgeCount)
        {
            ArrayUtils.Swap(extEdges, i, j);

            for (int k = 0; k < extEdgeCount; k++)
            {
                ref var edge = ref extEdges[k];
                if (edge.Prev == i)
                {
                    edge.Prev = j;
                }
                else if (edge.Prev == j)
                {
                    edge.Prev = i;
                }

                if (edge.Next == i)
                {
                    edge.Next = j;
                }
                else if (edge.Next == j)
                {
                    edge.Next = i;
                }
            }
        }

        private void SetInnerEdgeTrianglesColor(InnerEdgeData edgeData, Color color)
        {
            GetTriangleRef(edgeData.Triangle1Key, out _).FillColor = color;
            GetTriangleRef(edgeData.Triangle2Key, out _).FillColor = color;
        }
    }
}
