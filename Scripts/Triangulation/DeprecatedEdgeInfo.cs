using System;

namespace Triangulation
{
    public class DeprecatedEdgeInfo : EdgeInfo
    {
        public DeprecatedEdgeInfo(int edgeCapacity, TriangleSet triangleSet, Vector2[] points) : base(edgeCapacity, triangleSet, points)
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

        //public void ReplacePeakExternalEdges(EdgePeak peak, EdgeInfo addedEdgeInfo, List<int> pointsToClear, out IndexRange peaksRange)
        //{
        //    var addedExtEdges = addedEdgeInfo.extEdges;
        //    var addedPeakRange = addedEdgeInfo.GetPeakExternalEdgesRange(peak, this, pointsToClear);
        //    var removedRange = GetBaseExternalEdgesRange(addedExtEdges, addedPeakRange);
        //
        //    Log.WriteLine(GetType() + ".ReplacePeakExternalEdges: " + peak + " removedRange: " + removedRange + " addedPeakRange: " + addedPeakRange);
        //    if (removedRange.GetIndexCount() < extEdgeCount)
        //    {
        //        int addedCount = addedEdgeInfo.extEdgeCount = ArrayUtils.CutRange(addedExtEdges, addedPeakRange, addedExtEdges);
        //
        //        //PrintExternalEdges("ReplacePeakExternalEdges 1");
        //        InsertExternalEdges(addedExtEdges, addedCount, removedRange, true);
        //        //PrintExternalEdges("ReplacePeakExternalEdges 2");
        //
        //        int peaksBeg = addedPeakRange.Beg;
        //        int peaksEnd = addedPeakRange.GetPrev(addedPeakRange.End);
        //        peaksRange = new IndexRange(peaksBeg, peaksEnd, addedPeakRange.FullLength);
        //    }
        //    else
        //    {
        //        extEdgeCount = 0;
        //        peaksRange = IndexRange.None;
        //    }
        //}

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

        //public void AddExternalEdgesToTriangleDict(Triangle[] triangles, int trianglesCount, Color innerColor)
        //{
        //    for (int i = 0; i < trianglesCount; i++)
        //    {
        //        AddExternalEdgesToTriangleDict(ref triangles[i], out _, innerColor);
        //    }
        //}

        //public void AddExternalEdgesToTriangleDict(ref Triangle triangle, out bool isTriangleExternal, Color innerColor)
        //{
        //    if (triangle.Key < 0)
        //    {
        //        throw new Exception("AddExternalEdgesToTriangleDict: " + triangle.Key);
        //    }
        //    bool isExternal = false;
        //    long triangleKey = triangle.Key;
        //    ForEachEdge(triangle, (edgeKey, edge) => {
        //        if (IsEdgeExternal(edgeKey))
        //        {
        //            isExternal = true;
        //            extEdgeTriangleDict.Add(edgeKey, triangleKey);
        //        }
        //    });
        //    isTriangleExternal = isExternal;
        //    triangle.FillColor = isExternal ? Color.LightGreen : innerColor;
        //}

        //public bool AddExternalTriangles(IndexRange extEdgesRange, int pointIndex, Func<EdgeEntry, int, bool> addTriangle)
        //{
        //    bool triangleAdded = false;
        //    var range = extEdgesRange;
        //    int edgeIndex = range.Beg;
        //    int terminalIndex = extEdges[range.End].Next;
        //    if (terminalIndex == edgeIndex)
        //    {
        //        throw new Exception("AddExternalTriangles: terminalIndex == range.Beg: " + terminalIndex);
        //    }
        //    while (edgeIndex != terminalIndex)
        //    {
        //        var edge = extEdges[edgeIndex];
        //        if (addTriangle(edge, pointIndex))
        //        {
        //            triangleAdded = true;
        //        }
        //        else if (triangleAdded)
        //        {
        //            var prevEdge = extEdges[edge.Prev];
        //            int sharedIndex = EdgeEntry.GetSharedVertex(edge, prevEdge);
        //            var sharedVertex = points[sharedIndex];
        //            var point = points[pointIndex];
        //            var pointRay = point - sharedVertex;
        //            var edgeVec = edge.GetVector(points, sharedIndex == edge.B);
        //            float dotRayEdge = Vector2.Dot(pointRay, edgeVec);
        //            return Math.Sign(dotRayEdge) < 0;
        //        }
        //        edgeIndex = edge.Next;
        //    }
        //    return triangleAdded;
        //}

        //public bool GetFirstEdgeWithPredicate(Triangle triangle, Func<EdgeEntry, int, bool> predicate, out EdgeEntry edge, out int edgeKey)
        //{
        //    triangle.GetEdges(edgeBuffer);
        //    for (int i = 0; i < 3; i++)
        //    {
        //        if (predicate(edgeBuffer[i], edgeKey = GetEdgeKey(edgeBuffer[i])))
        //        {
        //            edge = edgeBuffer[i];
        //            return true;
        //        }
        //    }
        //    edgeKey = -1;
        //    edge = EdgeEntry.None;
        //    return false;
        //}

        //public bool HasExternalPoint(Triangle triangle)
        //{
        //    triangle.GetIndices(indexBuffer);
        //    for (int i = 0; i < 3; i++)
        //    {
        //        int pointIndex = indexBuffer[i];
        //        if (pointsExternal[pointIndex])
        //        {
        //            return true;
        //        }
        //    }
        //    return false;
        //}

        //public bool GetFirstExternalEdgeInRange(IndexRange range, Predicate<EdgeEntry> predicate, out EdgeEntry extEdge)
        //{
        //    EdgeEntry edge;
        //    int edgeIndex = range.Beg;
        //    int terminalIndex = extEdges[range.End].Next;
        //    extEdge = EdgeEntry.None;
        //    while (edgeIndex != terminalIndex)
        //    {
        //        edge = extEdges[edgeIndex];
        //        if (predicate(edge))
        //        {
        //            extEdge = edge;
        //            return true;
        //        }
        //        edgeIndex = edge.Next;
        //    }
        //    return false;
        //}

        //public IndexRange InvokeForPointsInOppositeEdgesRange(int addedPointIndex, ICollection<long> triangleKeys, Action<int> action)
        //{
        //    if (GetFirstExternalEdge(triangleKeys, out int extEdgeIndex, out var triangle))
        //    {
        //        int beg, end;
        //        var point = points[addedPointIndex];
        //        bool isPointOpposite = IsPointOppositeToExternalEdge(point, extEdgeIndex, triangle);
        //        if (isPointOpposite)
        //        {
        //            beg = InvokeForNextOppositeEdges(point, extEdgeIndex, false, true, action);
        //            end = InvokeForNextOppositeEdges(point, extEdgeIndex, true, true, action);
        //        }
        //        else
        //        {
        //            extEdgeIndex = GetFirstEdgeOppositeToExternalPoint(point, extEdgeIndex, out bool isPointOppositeToNext);
        //            if (extEdgeIndex >= 0)
        //            {
        //                end = InvokeForNextOppositeEdges(point, extEdgeIndex, isPointOppositeToNext, true, action);
        //                InvokeForNextEdge(extEdgeIndex, !isPointOppositeToNext, action);
        //
        //                if (isPointOppositeToNext)
        //                {
        //                    beg = extEdgeIndex;
        //                }
        //                else
        //                {
        //                    beg = end;
        //                    end = extEdgeIndex;
        //                }
        //            }
        //            else
        //            {
        //                beg = end = -1;
        //            }
        //        }
        //        return extEdgeIndex >= 0 ? new IndexRange(beg, end, extEdgeCount) : IndexRange.None;
        //    }
        //    else
        //    {
        //        throw new Exception("!GetFirstExternalEdge");
        //    }
        //}

        //private int InvokeForNextEdge(int extEdgeIndex, bool forward, Action<int> action)
        //{
        //    int sharedVertIndex = GetNextEdgeSharedVertex(extEdgeIndex, forward, out int nextEdgeIndex);
        //
        //    //AddCellTrianglesVertsToCellPoints(sharedVertIndex);
        //    action(sharedVertIndex);
        //
        //    return nextEdgeIndex;
        //}

        //private int InvokeForNextOppositeEdges(Vector2 point, int extEdgeIndex, bool forward, bool skipFirstCheck, Action<int> action)
        //{
        //    int next = extEdgeIndex;
        //    while (skipFirstCheck || IsPointOppositeToExternalEdge(point, next))
        //    {
        //        next = InvokeForNextEdge(extEdgeIndex = next, forward, action);
        //        skipFirstCheck = false;
        //    }
        //    return extEdgeIndex;
        //}

        //private int GetAddedExternalEdges(Triangle[] addedTriangles, int addedTrianglesCount, EdgeEntry[] addedExtEdges)
        //{
        //    int counter = 0;
        //    if (addedTrianglesCount <= 0)
        //    {
        //        return 0;
        //    }
        //    else if (addedTrianglesCount == 1)
        //    {
        //        ForEachExternalEdge(addedTriangles[0], (edgeKey, edge) => {
        //            addedExtEdges[counter++] = edge;
        //        });
        //    }
        //    else
        //    {
        //        for (int i = 0; i < addedTrianglesCount; i += addedTrianglesCount - 1)
        //        {
        //            if (GetFirstExternalEdge(addedTriangles[i], out var edge))
        //            {
        //                addedExtEdges[counter++] = edge;
        //            }
        //        }
        //    }
        //    if (counter != 2)
        //    {
        //        throw new Exception("GetAddedExternalEdges: counter: " + counter + " addedTrianglesCount: " + addedTrianglesCount);
        //    }
        //    return counter;
        //}

        //private void InsertExternalEdges(int addedExtEdgeCount, EdgeEntry[] addedExtEdges, List<IndexRange> removedRanges, List<IndexRange> addedRanges)
        //{
        //    if (addedExtEdgeCount <= 0 || removedRanges.Count == 0)
        //    {
        //        return;
        //    }
        //    removedRanges.Sort((r1, r2) => r1.Beg.CompareTo(r2.Beg));
        //
        //    int totalCount = extEdgeCount;
        //    int removedCount = 0;
        //
        //    removedRanges.ForEach(range => removedCount += range.GetIndexCount());
        //
        //    bool tryInvert = removedCount < extEdgeCount;
        //
        //    for (int i = removedRanges.Count - 1; i >= 0; i--)
        //    {
        //        var removedRange = removedRanges[i];
        //        var removedBegEdge = extEdges[removedRange.Beg];
        //        var removedEndEdge = extEdges[removedRange.End];
        //
        //        for (int j = addedRanges.Count - 1; j >= 0; j--)
        //        {
        //            var addedRange = addedRanges[j];
        //            var addedBegEdge = addedExtEdges[addedRange.Beg];
        //            var addedEndEdge = addedExtEdges[addedRange.End];
        //
        //            if (removedBegEdge.SharesVertex(addedBegEdge) || removedBegEdge.SharesVertex(addedEndEdge))
        //            {
        //                if (!removedEndEdge.SharesVertex(addedBegEdge) && !removedEndEdge.SharesVertex(addedEndEdge))
        //                {
        //                    throw new Exception("removedBegEnd: " + removedBegEdge + " " + removedEndEdge + " addedBegEnd: " + addedBegEdge + " " + addedEndEdge);
        //                }
        //                totalCount = InsertExternalEdges(addedExtEdges, addedRange, removedRange, totalCount, tryInvert);
        //                addedRanges.RemoveAt(j);
        //            }
        //        }
        //    }
        //
        //    var lastRemovedRange = removedRanges[removedRanges.Count - 1];
        //    if (lastRemovedRange.Beg > lastRemovedRange.End)
        //    {
        //        int removedBegIndex = lastRemovedRange.Beg;
        //        int removedEndIndex = lastRemovedRange.End;
        //        totalCount = totalCount - 1 - removedEndIndex;
        //        Array.Copy(extEdges, removedEndIndex + 1, extEdges, 0, totalCount);
        //    }
        //
        //    extEdgeCount = totalCount;
        //    RefreshExternalEdgesNextPrev();
        //
        //    //Log.WriteLine(GetType() + ".InsertExternalEdges");
        //    //Log.PrintEdges(extEdges, extEdgeCount, "final extEdges");
        //}

        //private int InsertExternalEdges(EdgeEntry[] addedExtEdges, IndexRange addedRange, IndexRange removedRange, int totalCount, bool tryInvert)
        //{
        //    Log.WriteLine(GetType() + ".InsertExternalEdges: removedRange: " + removedRange + " addedRange: " + addedRange);
        //    //Log.WriteLine(GetType() + ".InsertExternalEdges");
        //    //Log.PrintEdges(extEdges, extEdgeCount, "extEdges");
        //    //Log.WriteLine(GetType() + ".InsertExternalEdges");
        //    //Log.PrintEdges(addedExtEdges, addedExtEdgeCount, "addedExtEdges");
        //
        //    int addedCount = addedRange.GetIndexCount();
        //
        //    int removedBeg = removedRange.Beg;
        //    int removedEnd = removedRange.End;
        //    int removedCount;
        //
        //    if (tryInvert)
        //    {
        //        int validPrev = IndexRange.GetPrev(removedBeg, totalCount);
        //        var validPrevEdge = extEdges[validPrev];
        //        var addedBegEdge = addedExtEdges[addedRange.Beg];
        //        if (!validPrevEdge.SharesVertex(addedBegEdge))
        //        {
        //            //Log.WriteLine(GetType() + ".InsertExternalEdges: INVERT ADDED EDGES ORDER: " + validPrev + " " + validPrevEdge);
        //            //Log.PrintEdges(addedExtEdges, addedCount);
        //            ArrayUtils.InvertOrder(addedExtEdges, addedRange.Beg, addedRange.End);
        //            //Log.PrintEdges(addedExtEdges, addedCount, "***");
        //        }
        //    }
        //    if (removedEnd >= removedBeg)
        //    {
        //        removedCount = removedEnd - removedBeg + 1;
        //
        //        int tailDestIndex = removedBeg + addedCount;
        //        int tailLength = totalCount - 1 - removedEnd;
        //        Array.Copy(extEdges, removedEnd + 1, extEdges, tailDestIndex, tailLength);
        //        // Next ranges are not in order with array after this copy.
        //    }
        //    else
        //    {
        //        removedCount = extEdgeCount - removedBeg;
        //        // Don't copy range to array start to preserve order for next ranges.
        //    }
        //    Array.Copy(addedExtEdges, addedRange.Beg, extEdges, removedBeg, addedCount);
        //
        //    totalCount = totalCount - removedCount + addedCount;
        //    return totalCount;
        //}

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
