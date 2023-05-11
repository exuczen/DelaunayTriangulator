using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;

namespace Triangulation
{
    public class IncrementalEdgeInfo : EdgeInfo
    {
        public int ExtEdgeCount => extEdgeCount;
        public EdgeEntry[] ExtEdges => extEdges;
        public EdgeEntry[] AddedExtEdges => addedExtEdges;
        public int InnerEdgeCount => innerEdgeTriangleDict.Count;

        public Color ExtTriangleColor = Color.LightGreen;

        protected readonly Dictionary<int, long> extEdgeTriangleDict = new Dictionary<int, long>();
        protected readonly Dictionary<int, InnerEdgeData> innerEdgeTriangleDict = new Dictionary<int, InnerEdgeData>();
        protected readonly EdgeEntry[] extEdges = null;
        protected readonly EdgeEntry[] addedExtEdges = null;
        protected readonly int[] indexBuffer = new int[3];
        protected readonly long[] triangleKeyBuffer = new long[3];

        protected readonly TriangleSet triangleSet = null;

        protected readonly List<IndexRange> extEdgeRanges = new List<IndexRange>();

        protected readonly List<int> lastPointExtEdgeIndices = new List<int>();

        protected int extEdgeCount = 0;

        public IncrementalEdgeInfo(TriangleSet triangleSet, Vector2[] points, IExceptionThrower exceptionThrower) : base(points, exceptionThrower)
        {
            extEdges = new EdgeEntry[points.Length << 2];
            addedExtEdges = new EdgeEntry[1024];

            this.triangleSet = triangleSet;
        }

        public IncrementalEdgeInfo(Vector2[] points, IExceptionThrower exceptionThrower) : this(null, points, exceptionThrower) { }

        #region Logs
        public void PrintExtEdgeTriangleDict(string prefix = null)
        {
            PrintEdgeDict(extEdgeTriangleDict, ".PrintExtEdgeTriangleDict: " + extEdgeTriangleDict.Count + " " + prefix);
        }

        public void PrintInnerEdgeTriangleDict(string prefix = null)
        {
            PrintEdgeDict(innerEdgeTriangleDict, ".PrintInnerEdgeTriangleDict: " + innerEdgeTriangleDict.Count + " " + prefix);
        }

        public void PrintExternalEdges(string prefix = null)
        {
            Log.PrintArray(extEdges, extEdgeCount, "PrintExternalEdges: " + prefix);
        }

        public void PrintPointsExternal(int pointsCount, string prefix = null)
        {
            Log.WriteLine(GetType() + ".PrintPointsExternal: " + prefix);
            for (int i = 0; i < pointsCount; i++)
            {
                if (pointsExternal[i])
                {
                    Log.WriteLine(i + " " + pointsExternal[i]);
                }
            }
            for (int i = 0; i < pointsCount; i++)
            {
                if (!pointsExternal[i])
                {
                    Log.WriteLine(i + " " + pointsExternal[i]);
                }
            }
        }
        #endregion

        public void Clear()
        {
            extEdgeRanges.Clear();
            edgeCounterDict.Clear();
            innerEdgeTriangleDict.Clear();
            extEdgeTriangleDict.Clear();
            extEdgeCount = 0;
        }

        public int GetExternalTrianglesCount()
        {
            if (extEdgeCount < 3)
            {
                return 0;
            }
            if (!TryGetExternalTriangleKey(extEdges[0], out long firstKey, out _))
            {
                throw new KeyNotFoundException("GetExternalTrianglesCount: " + extEdges[0]);
            }
            long prevKey = firstKey;
            int count = 0;
            for (int i = 1; i < extEdgeCount; i++)
            {
                if (TryGetExternalTriangleKey(extEdges[i], out long key, out _))
                {
                    if (key != prevKey)
                    {
                        count++;
                    }
                    prevKey = key;
                }
                else
                {
                    throw new KeyNotFoundException("GetExternalTrianglesCount: " + extEdges[0]);
                }
            }
            if (firstKey != prevKey)
            {
                count++;
            }
            return Math.Max(count, 1);
        }

        public void ForEachPointInExtEdgesRange(IndexRange range, Action<int> action)
        {
            var prevEdge = GetPrevExtEdge(range.Beg);
            InvokeForExternalEdgesRange(range, edge => {
                action(EdgeEntry.GetSharedVertex(edge, prevEdge));
                prevEdge = edge;
            });
            var endEdge = extEdges[range.End];
            var nextEdge = GetNextExtEdge(range.End);
            action(EdgeEntry.GetSharedVertex(endEdge, nextEdge));
        }

        public void ForEachExternalPoint(Action<int> action, out bool openLoop)
        {
            openLoop = false;
            if (extEdgeCount <= 0)
            {
                return;
            }
            void extEdgePeakAction(int edgeI, int edgeJ, ref bool openLoop)
            {
                int sharedVertex = EdgeEntry.GetSharedVertex(extEdges[edgeI], extEdges[edgeJ]);
                if (sharedVertex >= 0)
                {
                    action(sharedVertex);
                }
                else
                {
                    openLoop = true;
                }
            }
            for (int i = 1; i < extEdgeCount; i++)
            {
                extEdgePeakAction(i - 1, i, ref openLoop);
            }
            extEdgePeakAction(extEdgeCount - 1, 0, ref openLoop);
        }

        public void ForEachExternalPeak(Action<EdgeEntry, EdgeEntry> action)
        {
            if (extEdgeCount <= 0)
            {
                return;
            }
            for (int i = 1; i < extEdgeCount; i++)
            {
                action(extEdges[i - 1], extEdges[i]);
            }
            action(extEdges[extEdgeCount - 1], extEdges[0]);
        }

        public void ForEachExternalEdge(Action<EdgeEntry, int> action)
        {
            for (int i = 0; i < extEdgeCount; i++)
            {
                action(extEdges[i], GetEdgeKey(extEdges[i]));
            }
        }

        public bool ForEachExternalEdge(Predicate<EdgeEntry> predicate, bool breakResult = false)
        {
            for (int i = 0; i < extEdgeCount; i++)
            {
                if (!predicate(extEdges[i]))
                {
                    return breakResult;
                }
            }
            return true;
        }

        public void SwapExternalEdges(int a, int b)
        {
            if (a >= 0 && a < extEdgeCount && b >= 0 && b < extEdgeCount)
            {
                ArrayUtils.Swap(extEdges, a, b);
            }
        }

        public int AddExternalEdge(EdgeEntry edge)
        {
            extEdges[extEdgeCount++] = edge;
            return extEdgeCount;
        }

        public bool ForEachInnerEdge(Func<int, InnerEdgeData, bool> predicate)
        {
            foreach (var kvp in innerEdgeTriangleDict)
            {
                if (!predicate(kvp.Key, kvp.Value))
                {
                    return false;
                }
            }
            return true;
        }

        public void ForEachInnerEdgeWithCCPair(Action<EdgeEntry, Vector2, Vector2> action)
        {
            var edgesData = innerEdgeTriangleDict.Values;
            foreach (var edgeData in edgesData)
            {
                ref var t1 = ref GetTriangleRef(edgeData.Triangle1Key, out _);
                ref var t2 = ref GetTriangleRef(edgeData.Triangle2Key, out _);
                action(edgeData.Edge, t1.CircumCircle.Center, t2.CircumCircle.Center);
            }
        }

        public void ForEachCircumCirclePair(Action<Vector2, Vector2> action)
        {
            foreach (var kvp in innerEdgeTriangleDict)
            {
                ref var t1 = ref GetTriangleRef(kvp.Value.Triangle1Key, out _);
                ref var t2 = ref GetTriangleRef(kvp.Value.Triangle2Key, out _);
                action(t1.CircumCircle.Center, t2.CircumCircle.Center);
            }
        }

        public void SetInnerEdgeData(int edgeKey, InnerEdgeData edgeData)
        {
            innerEdgeTriangleDict[edgeKey] = edgeData;
        }

        public bool GetInnerEdgeData(int edgeKey, out InnerEdgeData edgeData)
        {
            return innerEdgeTriangleDict.TryGetValue(edgeKey, out edgeData);
        }

        public bool IsPointExternal(int pointIndex)
        {
            return pointsExternal[pointIndex];
        }

        public void RemoveEdgesFromDicts(Triangle triangle)
        {
            ForEachEdgeInCounterDict(triangle, (edgeKey, edge, edgeCount) => {
                if (edgeCount == 2)
                {
                    RemoveInnerEdgeTriangleFromDict(edgeKey, triangle.Key);
                    edgeCounterDict[edgeKey]--;
                }
                else if (edgeCount == 1)
                {
                    edgeCounterDict.Remove(edgeKey);
                    extEdgeTriangleDict.Remove(edgeKey);
                    innerEdgeTriangleDict.Remove(edgeKey);
                }
                else
                {
                    throw new Exception("RemoveEdgesFromDicts: " + triangle + " | " + edge);
                }
            });
        }

        public bool HasExternalEdge(Triangle triangle)
        {
            return GetFirstExternalEdge(triangle, out _);
        }

        public bool HasInternalEdge(Triangle triangle)
        {
            return GetFirstInternalEdge(triangle, out _);
        }

        public bool GetFirstInternalEdge(Triangle triangle, out EdgeEntry internalEdge)
        {
            return GetFirstEdgeWithPredicate(triangle, IsEdgeInternal, out internalEdge, out _);
        }

        public bool GetFirstExternalEdgeIndex(Triangle triangle, out int edgeIndex)
        {
            if (GetFirstExternalEdge(triangle, out var extEdge))
            {
                edgeIndex = GetExternalEdgeIndex(extEdge);
                return true;
            }
            edgeIndex = -1;
            return false;
        }

        public bool GetFirstExternalEdge(Triangle triangle, out EdgeEntry externalEdge)
        {
            return GetFirstEdgeWithPredicate(triangle, IsEdgeExternal, out externalEdge, out _);
        }

        public bool GetFirstExternalEdgeOppositeToExternalPoint(Vector2 point, Triangle triangle, out int extEdgeIndex, out bool pointOnEdge, string logPrefix)
        {
            triangle.GetEdges(edgeBuffer);
            for (int i = 0; i < 3; i++)
            {
                var extEdge = edgeBuffer[i];
                if (IsEdgeExternal(extEdge))
                {
                    bool opposite = IsPointOppositeToExternalEdge(point, extEdge, -1, out pointOnEdge, logPrefix);
                    if (opposite || pointOnEdge)
                    {
                        extEdgeIndex = GetExternalEdgeIndex(extEdge);
                        return true;
                    }
                }
            }
            extEdgeIndex = -1;
            pointOnEdge = false;
            return false;
        }

        public bool GetFirstEdgeWithPredicate(Triangle triangle, Predicate<EdgeEntry> predicate, out EdgeEntry edge)
        {
            triangle.GetEdges(edgeBuffer);
            for (int i = 0; i < 3; i++)
            {
                if (predicate(edgeBuffer[i]))
                {
                    edge = edgeBuffer[i];
                    return true;
                }
            }
            edge = EdgeEntry.None;
            return false;
        }

        public bool GetFirstEdgeWithPredicate(Triangle triangle, Predicate<int> predicate, out EdgeEntry edge, out int edgeKey)
        {
            triangle.GetEdges(edgeBuffer);
            for (int i = 0; i < 3; i++)
            {
                if (predicate(edgeKey = GetEdgeKey(edgeBuffer[i])))
                {
                    edge = edgeBuffer[i];
                    return true;
                }
            }
            edgeKey = -1;
            edge = EdgeEntry.None;
            return false;
        }

        public void ClearTrianglesPointsExternal(Triangle[] addedTriangles, int addedTrianglesCount)
        {
            for (int i = 0; i < addedTrianglesCount; i++)
            {
                SetTrianglePointsExternal(addedTriangles[i], false);
            }
        }

        public void InsertExternalEdges(EdgePeak edgePeak, IndexRange removedRange)
        {
            int addedExtEdgesCount = 2;
            addedExtEdges[0] = edgePeak.EdgeA;
            addedExtEdges[1] = edgePeak.EdgeB;
            InsertExternalEdges(addedExtEdges, addedExtEdgesCount, removedRange, true);
        }

        protected bool InsertExternalEdges(EdgeEntry[] addedExtEdges, int addedCount, IndexRange removedRange, bool setPointsExternal)
        {
            if (addedCount <= 0)
            {
                return false;
            }
            int removedBeg = removedRange.Beg;
            int removedEnd = removedRange.End;
            int removedCount = removedRange.GetIndexCount();
            int totalCount = extEdgeCount;
            int addedDestIndex;

            // try invert addedExtEdges
            {
                int validPrev = extEdges[removedBeg].Prev;
                var validPrevEdge = extEdges[validPrev];
                var addedBegEdge = addedExtEdges[0];
                var addedEndEdge = addedExtEdges[addedCount - 1];
                if (!validPrevEdge.SharesVertex(addedBegEdge))
                {
                    if (!validPrevEdge.SharesVertex(addedEndEdge))
                    {
                        throw new Exception("InsertExternalEdges: !validPrevEdge.SharesVertex(addedEndEdge): " + validPrevEdge + " " + addedEndEdge);
                    }
                    //Log.WriteLine(GetType() + ".InsertExternalEdges: INVERT ADDED EDGES ORDER: " + validPrev + " " + validPrevEdge);
                    //Log.PrintEdges(addedExtEdges, addedCount);
                    ArrayUtils.InvertOrder(addedExtEdges, 0, addedCount - 1);
                    //Log.PrintEdges(addedExtEdges, addedCount, "***");
                }
            }
            // set edge points external
            if (setPointsExternal)
            {
                InvokeForExternalEdgesRange(removedRange, edge => SetEdgePointsExternal(edge, false));

                for (int i = 0; i < addedCount; i++)
                {
                    SetEdgePointsExternal(addedExtEdges[i], true);
                }
            }
            // replace edges
            {
                if (removedEnd >= removedBeg)
                {
                    int tailDestIndex = removedBeg + addedCount;
                    int tailLength = totalCount - 1 - removedEnd;
                    Array.Copy(extEdges, removedEnd + 1, extEdges, tailDestIndex, tailLength);

                    addedDestIndex = removedBeg;
                }
                else
                {
                    int restCount = totalCount - removedCount;
                    Array.Copy(extEdges, removedEnd + 1, extEdges, 0, restCount);

                    addedDestIndex = restCount;
                }
                Array.Copy(addedExtEdges, 0, extEdges, addedDestIndex, addedCount);

                extEdgeCount += addedCount - removedCount;
            }
            RefreshExternalEdgesNextPrev();

            return addedCount > 0;
        }

        public bool InsertExternalEdgesWithPointOnEdge(DegenerateTriangle triangle, int extEdgeIndex)
        {
            var extEdge = triangle.Edge;
            int pointIndex = triangle.PointIndex;

            if (extEdgeIndex >= 0 && !extEdge.Equals(extEdges[extEdgeIndex]))
            {
                throw new Exception("InsertExternalEdgesWithPointOnEdge: " + extEdge + " != " + extEdges[extEdgeIndex]);
            }
            else if (!IsEdgeExternal(extEdge, out _))
            {
                throw new Exception("InsertExternalEdgesWithPointOnEdge: !IsEdgeExternal: extEdge: " + extEdge + " pointIndex: " + pointIndex);
            }
            else if (!extEdge.IsPointInRange(pointIndex, points))
            {
                throw new Exception("InsertExternalEdgesWithPointOnEdge: !IsPointInRange: " + extEdge + " pointIndex: " + pointIndex);
            }
            else
            {
                if (extEdgeIndex < 0)
                {
                    extEdgeIndex = GetExternalEdgeIndex(extEdge);
                    //Log.WriteLine(GetType() + ".InsertExternalEdgesWithPointOnEdge: IsPointOppositeToExternalEdge: " + IsPointOppositeToExternalEdge(points[pointIndex], extEdgeIndex, out _));
                }
                Log.WriteLine(GetType() + ".InsertExternalEdgesWithPointOnEdge: extEdge: " + extEdge + " pointIndex: " + pointIndex);
                edgeBuffer[0] = new EdgeEntry(extEdge.A, pointIndex);
                edgeBuffer[1] = new EdgeEntry(extEdge.B, pointIndex);

                int prev = extEdges[extEdgeIndex].Prev;
                if (!extEdges[prev].SharesVertex(edgeBuffer[0]))
                {
                    if (!extEdges[prev].SharesVertex(edgeBuffer[1]))
                    {
                        throw new Exception("InsertExternalEdgesWithPointOnEdge: " + extEdges[prev] + " " + edgeBuffer[0] + " " + edgeBuffer[1]);
                    }
                    ArrayUtils.Swap(edgeBuffer, 0, 1);
                }
                Array.Copy(extEdges, extEdgeIndex, extEdges, extEdgeIndex + 1, extEdgeCount - extEdgeIndex);
                extEdges[extEdgeIndex] = edgeBuffer[0];
                extEdges[extEdgeIndex + 1] = edgeBuffer[1];
                extEdgeCount++;
                SetPointExternal(pointIndex, true);
                RefreshExternalEdgesNextPrev(extEdgeIndex);
                return true;
            }
        }

        private IndexRange GetPeakExternalEdgesRange(EdgePeak peak)
        {
            int edgeIndex = GetExternalEdgeIndex(peak.EdgeA);
            var edgeB = peak.EdgeB;
            int beg = extEdges[edgeIndex].Prev;
            int end = extEdges[edgeIndex].Next;
            if (edgeB.Equals(extEdges[end]))
            {
                beg = edgeIndex;
            }
            else if (edgeB.Equals(extEdges[beg]))
            {
                end = edgeIndex;
            }
            else
            {
                throw new Exception("RemovePeakExternalEdges: " + peak);
            }
            return new IndexRange(beg, end, extEdgeCount);
        }

        public IndexRange GetPeakExternalEdgesRange(EdgePeak peak, IncrementalEdgeInfo baseEdgeInfo, List<int> pointsToClear)
        {
            var range = GetPeakExternalEdgesRange(peak);
            int beg = range.Beg;
            int end = range.End;
            int prev = extEdges[beg].Prev;
            int next = extEdges[end].Next;
            var prevEdge = extEdges[prev];
            var nextEdge = extEdges[next];
            int prevVertex = extEdges[beg].GetOtherVertex(peak.PeakVertex);
            int nextVertex = extEdges[end].GetOtherVertex(peak.PeakVertex);
            pointsToClear.Add(peak.PeakVertex);

            while (prev != end && baseEdgeInfo.IsEdgeExternal(prevEdge, out _))
            {
                //Log.WriteLine(GetType() + ".GetPeakExternalEdgesRange: prevEdge: " + prevEdge + " prevVertex: " + prevVertex);
                beg = prev;
                pointsToClear.Add(prevVertex);
                prevVertex = prevEdge.GetOtherVertex(prevVertex);
                prevEdge = extEdges[prev = prevEdge.Prev];
            }
            if (next == beg)
            {
                //Log.WriteLine(GetType() + ".GetPeakExternalEdgesRange: next == beg: " + next + " nextVertex: " + nextVertex);
                pointsToClear.Add(nextVertex);
            }
            while (next != beg && baseEdgeInfo.IsEdgeExternal(nextEdge, out _))
            {
                //Log.WriteLine(GetType() + ".GetPeakExternalEdgesRange: nextEdge: " + nextEdge + " nextVertex: " + nextVertex);
                end = next;
                pointsToClear.Add(nextVertex);
                nextVertex = nextEdge.GetOtherVertex(nextVertex);
                nextEdge = extEdges[next = nextEdge.Next];
            }
            return new IndexRange(beg, end, extEdgeCount);
        }

        protected IndexRange GetBaseExternalEdgesRange(EdgeEntry[] addedExtEdges, IndexRange addedPeakRange)
        {
            int addedBeg = addedPeakRange.Beg;
            int addedEnd = addedPeakRange.End;
            var addedBegEdge = addedExtEdges[addedBeg];
            var addedBegNextEdge = addedExtEdges[addedBegEdge.Next];
            var addedEndEdge = addedExtEdges[addedEnd];

            int peakEdgeCount = addedPeakRange.GetIndexCount();

            int beg = GetExternalEdgeIndex(addedBegEdge);
            int end;
            int terminal;

            var begPrevEdge = GetPrevExtEdge(beg);
            var begNextEdge = GetNextExtEdge(beg);

            if (begPrevEdge.Equals(addedBegNextEdge))
            {
                end = beg;
                beg = terminal = (beg - peakEdgeCount + 1 + extEdgeCount) % extEdgeCount;
            }
            else if (begNextEdge.Equals(addedBegNextEdge))
            {
                end = terminal = (beg + peakEdgeCount - 1 + extEdgeCount) % extEdgeCount;
            }
            else
            {
                throw new Exception("GetPeakExternalEdgesRange: addedPeakRange: " + addedPeakRange + " beg: " + beg);
            }
            if (!extEdges[terminal].Equals(addedEndEdge))
            {
                throw new Exception("GetPeakExternalEdgesRange: addedPeakRange: " + addedPeakRange + " beg: " + beg);
            }
            return new IndexRange(beg, end, extEdgeCount);
        }

        public void ReplacePeakExternalEdges(IndexRange addedPeakRange, IncrementalEdgeInfo addedEdgeInfo)
        {
            var removedRange = GetBaseExternalEdgesRange(addedEdgeInfo.extEdges, addedPeakRange);
            //Log.WriteLine(GetType() + ".ReplacePeakExternalEdges: removedRange: " + removedRange + " addedPeakRange: " + addedPeakRange);
            if (removedRange.GetIndexCount() < extEdgeCount)
            {
                var addedExtEdges = addedEdgeInfo.addedExtEdges;
                int addedCount = EdgeEntry.GetLastElementCount(addedExtEdges);
                if (addedCount <= 0)
                {
                    addedCount = ArrayUtils.CutRange(addedEdgeInfo.extEdges, addedPeakRange, addedExtEdges);
                }
                //Log.PrintEdges(addedExtEdges, addedCount, "ReplacePeakExternalEdges: addedExtEdges: " + addedCount);
                //PrintExternalEdges("ReplacePeakExternalEdges(1): baseExtEdges: ");
                InsertExternalEdges(addedExtEdges, addedCount, removedRange, true);
                //PrintExternalEdges("ReplacePeakExternalEdges(2): baseExtEdges: ");

                EdgeEntry.SetLastElementCount(addedExtEdges, 0);
            }
            else
            {
                extEdgeCount = 0;
            }
        }

        public void ClipPeakExternalEdges(EdgePeak peak, List<int> pointsToClear)
        {
            pointsToClear?.Add(peak.PeakVertex);

            if (triangleSet.Count > 1)
            {
                var peakRange = GetPeakExternalEdgesRange(peak);

                var oppEdge = peak.GetOppositeEdge(out _);
                addedExtEdges[0] = oppEdge;

                InsertExternalEdges(addedExtEdges, 1, peakRange, true);
            }
            else
            {
                pointsToClear?.Add(peak.VertexA);
                pointsToClear?.Add(peak.VertexB);

                extEdgeCount = 0;
            }
        }

        public bool FindExternalEdges(int pointsCount, out string error)
        {
            //Log.WriteLine(GetType() + ".FindExternalEdges: ");

            extEdgeCount = 0;

            for (int i = 0; i < pointsCount; i++)
            {
                SetPointExternal(i, false);
            }
            //foreach (var kvp in edgeCounterDict)
            //{
            //    if (kvp.Value == 1)
            //    {
            //        var edge = extEdges[extEdgeCount++] = GetEdgeFromKey(kvp.Key);
            //        SetEdgePointsExternal(edge, true);
            //    }
            //}
            foreach (var kvp in extEdgeTriangleDict)
            {
                var edge = extEdges[extEdgeCount++] = GetEdgeFromKey(kvp.Key);
                SetEdgePointsExternal(edge, true);
            }
            return JoinSortExternalEdges(out error);
        }

        public void UpdateTriangleDicts(IncrementalEdgeInfo addedEdgeInfo)
        {
            addedEdgeInfo.ForEachExternalEdge((edge, edgeKey) => {
                if (GetInnerEdgeData(edgeKey, out var edgeData) && edgeData.IsTriangleClear)
                {
                    if (edgeData.IsClear)
                    {
                        throw new Exception("UpdateTriangleDicts: edgeData.IsClear: " + edgeData);
                    }
                    innerEdgeTriangleDict.Remove(edgeKey);

                    if (IsEdgeExternal(edgeKey))
                    {
                        long triangleKey = edgeData.GetValidTriangleKey();
                        extEdgeTriangleDict.Add(edgeKey, triangleKey);

                        ref var triangle = ref GetTriangleRef(triangleKey, out _);
                        triangle.FillColor = ExtTriangleColor;

                        Log.WriteLine(GetType() + ".UpdateTriangleDicts: " + edge + " changed from internal to external");
                    }
                    else
                    {
                        throw new Exception("UpdateTriangleDicts: !IsEdgeExternal: " + edge);
                    }
                }
            });
        }

        public void SetTrianglesColors(Color innerColor)
        {
            foreach (var kvp in innerEdgeTriangleDict)
            {
                var edgeData = kvp.Value;
                GetTriangleRef(edgeData.Triangle1Key, out _).FillColor = innerColor;
                GetTriangleRef(edgeData.Triangle2Key, out _).FillColor = innerColor;
            }
            foreach (var kvp in extEdgeTriangleDict)
            {
                GetTriangleRef(kvp.Value, out _).FillColor = ExtTriangleColor;
            }
        }

        public void AddEdgesToTriangleDicts(Triangle[] triangles, int trianglesCount)
        {
            for (int i = 0; i < trianglesCount; i++)
            {
                AddEdgesToTriangleDicts(ref triangles[i]);
            }
            //PrintExtEdgeTriangleDict();
            //PrintInnerEdgeTriangleDict();
        }

        public void AddEdgesToTriangleDicts(Triangle[] triangles, int trianglesCount, Color innerColor)
        {
            for (int i = 0; i < trianglesCount; i++)
            {
                AddEdgesToTriangleDicts(ref triangles[i], innerColor);
            }
            //PrintExtEdgeTriangleDict();
            //PrintInnerEdgeTriangleDict();
        }

        public void AddEdgesToTriangleDicts(ref Triangle triangle)
        {
            if (triangle.Key < 0)
            {
                throw new Exception("AddEdgesToTriangleDicts: " + triangle.Key);
            }
            long triangleKey = triangle.Key;

            ForEachEdgeInCounterDict(triangle, (edgeKey, edge, edgeCount) => {
                if (edgeCount == 1)
                {
                    extEdgeTriangleDict.Add(edgeKey, triangleKey);
                }
                else if (edgeCount == 2)
                {
                    AddInnerEdgeToTriangleDict(edge, edgeKey, triangleKey, out long extTriangleKey);
                }
            });
        }

        public void AddEdgesToTriangleDicts(ref Triangle triangle, Color innerColor)
        {
            if (triangle.Key < 0)
            {
                throw new Exception("AddEdgesToTriangleDicts: " + triangle.Key);
            }
            long triangleKey = triangle.Key;
            int extTriangleCount = 0;
            int extEdgeCount = 0;

            ForEachEdgeInCounterDict(triangle, (edgeKey, edge, edgeCount) => {
                if (edgeCount == 1)
                {
                    extEdgeTriangleDict.Add(edgeKey, triangleKey);
                    extEdgeCount++;
                }
                else if (edgeCount == 2)
                {
                    AddInnerEdgeToTriangleDict(edge, edgeKey, triangleKey, out long extTriangleKey);
                    if (extTriangleKey >= 0)
                    {
                        triangleKeyBuffer[extTriangleCount++] = extTriangleKey;
                    }
                }
            });
            triangle.FillColor = extEdgeCount > 0 ? ExtTriangleColor : innerColor;
            GetTriangleRef(triangleKey, out _).FillColor = triangle.FillColor;

            // change color of former external triangles
            {
                for (int i = 0; i < extTriangleCount; i++)
                {
                    ref var extTriangle = ref GetTriangleRef(triangleKeyBuffer[i], out _);
                    if (!HasExternalEdge(extTriangle))
                    {
                        extTriangle.FillColor = innerColor;
                    }
                }
            }
        }

        public bool JoinSortExternalEdges(out string errorMessage)
        {
            extEdgeRanges.Clear();
            errorMessage = null;

            if (extEdgeCount < 3)
            {
                return true;
            }
            //PrintExternalEdges("JoinSortExternalEdges 0");

            for (int i = 0; i < extEdgeCount; i++)
            {
                ref var edgeI = ref extEdges[i];
                for (int j = i + 1; j < extEdgeCount; j++)
                {
                    ref var edgeJ = ref extEdges[j];
                    if (edgeI.SharesVertex(edgeJ))
                    {
                        if (edgeI.Next >= 0)
                        {
                            edgeI.Prev = j;
                        }
                        else
                        {
                            edgeI.Next = j;
                        }

                        if (edgeJ.Prev >= 0)
                        {
                            edgeJ.Next = i;
                        }
                        else
                        {
                            edgeJ.Prev = i;
                        }
                    }
                }
                bool iNextNegative = edgeI.Next < 0;
                bool iPrevNegative = edgeI.Prev < 0;

                if (iNextNegative && !iPrevNegative)
                {
                    edgeI.SwapNextPrev();
                }
            }
            //PrintExternalEdges("JoinSortExternalEdges 1");

            if (extEdgeCount > 2)
            {
                int edgeCounter = 0;
                int offset = extEdgeCount;
                int firstIndex = 0;
                int edgeIndex = firstIndex;

                var range = new IndexRange(0, 0, extEdgeCount);

                while (edgeCounter < extEdgeCount)
                {
                    var extEdge = extEdges[edgeIndex];
                    if (extEdge.Count <= 0)
                    {
                        errorMessage = "JoinSortExternalEdges: INTERNAL LOOP EXTERNAL EDGE: " + extEdge;
                        break;
                    }
                    extEdges[edgeIndex].Count = 0;

                    int nextIndex = extEdge.Next;

                    range.End = edgeCounter;

                    extEdge.Prev = extEdge.Prev >= 0 ? edgeCounter - 1 : -1;

                    if (nextIndex == firstIndex || nextIndex < 0)
                    {
                        //Log.WriteLine(GetType() + ".JoinSortExternalEdges: ADD RANGE: " + range + " edgeCounter: " + edgeCounter + " " + extEdge);
                        extEdgeRanges.Add(range);

                        if (edgeCounter < extEdgeCount - 1 && GetFirstExtEdgeWithPredicate(edge => edge.Count > 0, out _, out int validEdgeIndex))
                        {
                            nextIndex = firstIndex = validEdgeIndex;
                            range = new IndexRange(edgeCounter + 1, edgeCounter + 1, extEdgeCount);
                        }
                        extEdge.Next = -1;
                    }
                    else
                    {
                        if (extEdges[nextIndex].Next == edgeIndex)
                        {
                            extEdges[nextIndex].SwapNextPrev();
                        }
                        extEdge.Next = edgeCounter + 1;
                    }
                    extEdges[offset + edgeCounter] = extEdge;

                    edgeIndex = nextIndex;
                    edgeCounter++;
                }
                Array.Copy(extEdges, offset, extEdges, 0, extEdgeCount);
            }

            if (extEdgeRanges.Count == 0)
            {
                extEdgeRanges.Add(new IndexRange(0, extEdgeCount - 1, extEdgeCount));
            }
            for (int i = 0; i < extEdgeRanges.Count; i++)
            {
                var range = extEdgeRanges[i];
                if (extEdges[range.Beg].SharesVertex(extEdges[range.End]))
                {
                    extEdges[range.Beg].Prev = range.End;
                    extEdges[range.End].Next = range.Beg;
                }
                else
                {
                    extEdges[range.Beg].Prev = -1;
                    extEdges[range.End].Next = -1;
                }
            }
            if (string.IsNullOrEmpty(errorMessage))
            {
                if (extEdgeRanges.Count > 1)
                {
                    errorMessage = "JoinSortExternalEdges: RANGES COUNT: " + extEdgeRanges.Count;
                }
                else if (!extEdges[0].SharesVertex(extEdges[extEdgeCount - 1]))
                {
                    errorMessage = "JoinSortExternalEdges: OPEN LOOP, extEdgeCount: " + extEdgeCount;
                }
            }
            if (!string.IsNullOrEmpty(errorMessage))
            {
                Log.WriteWarning(errorMessage);
                return false;
            }
            //PrintExternalEdges("JoinSortExternalEdges 2");
            //Log.PrintList(extEdgeRanges, "PrintIndexRanges: ");
            return true;
        }

        public bool ValidateExternalEdges(out string error)
        {
            error = null;
            if (extEdgeCount < 3)
            {
                return false;
            }
            void setExtEdgesPointsExternal(bool external)
            {
                ForEachExternalPeak((edge1, edge2) => {
                    SetEdgePointsExternal(edge1, external);
                    SetEdgePointsExternal(edge2, external);
                });
            }
            bool internalLoop = false;

            setExtEdgesPointsExternal(false);

            ForEachExternalPoint(pointIndex => {
                internalLoop |= IsPointExternal(pointIndex);
                SetPointExternal(pointIndex, true);
            }, out bool openLoop);

            if (openLoop)
            {
                setExtEdgesPointsExternal(true);
            }
            if (internalLoop || openLoop)
            {
                error = "internalLoop: " + internalLoop + " | openLoop: " + openLoop;
            }
            return !internalLoop && !openLoop;
        }

        public void ClearLastPointData()
        {
            for (int i = 0; i < lastPointExtEdgeIndices.Count; i++)
            {
                int edgeIndex = lastPointExtEdgeIndices[i];
                extEdges[edgeIndex].ClearLastPointData();
            }
            lastPointExtEdgeIndices.Clear();
        }

        public void InvokeForExternalEdgesRange(IndexRange range, Action<EdgeEntry> action)
        {
            int edgeIndex = range.Beg;
            while (edgeIndex != range.End)
            {
                action(extEdges[edgeIndex]);
                edgeIndex = extEdges[edgeIndex].Next;
            }
            action(extEdges[range.End]);
        }

        public bool InvokeForExternalEdgesRange(IndexRange range, Predicate<EdgeEntry> action, bool forward, out int lastValidIndex, out int invalidIndex)
        {
            var getNextEdgeIndex = GetNextExtEdgeIndex(forward);
            int edgeIndex, beg, end;
            if (forward)
            {
                beg = range.Beg;
                end = range.End;
            }
            else
            {
                beg = range.End;
                end = range.Beg;
            }
            edgeIndex = beg;
            invalidIndex = -1;
            lastValidIndex = -1;

            while (edgeIndex != end)
            {
                //Log.WriteLine(GetType() + ".InvokeForExternalEdgesRange: [" + edgeIndex + "] " + extEdges[edgeIndex].ToLastPointDataString(true));
                if (!action(extEdges[edgeIndex]))
                {
                    invalidIndex = edgeIndex;
                    return false;
                }
                lastValidIndex = edgeIndex;
                edgeIndex = getNextEdgeIndex(edgeIndex);
            }
            if (!action(extEdges[end]))
            {
                invalidIndex = end;
                return false;
            }
            lastValidIndex = end;
            return true;
        }

        private bool GetFirstExtEdgeInRange(IndexRange range, Predicate<EdgeEntry> predicate, bool forward, out int extEdgeIndex)
        {
            return !InvokeForExternalEdgesRange(range, edge => !predicate(edge), forward, out _, out extEdgeIndex);
        }

        private bool GetFirstExtEdgeWithPredicate(Predicate<EdgeEntry> predicate, out EdgeEntry extEdge, out int extEdgeIndex)
        {
            extEdge = EdgeEntry.None;
            extEdgeIndex = -1;
            for (int i = 0; i < extEdgeCount; i++)
            {
                if (predicate(extEdges[i]))
                {
                    extEdge = extEdges[i];
                    extEdgeIndex = i;
                    return true;
                }
            }
            return false;
        }

        private IndexRange TrimExternalEdgesRange(IndexRange range, out bool innerDegenerate)
        {
            innerDegenerate = false;

            static bool edgeInvalid(EdgeEntry edge) => edge.LastPointDegenerateTriangle || edge.LastPointDegenerateAngle || !edge.LastPointOpposite;

            int rangeCount = range.GetIndexCount();
            if (rangeCount == 1)
            {
                if (edgeInvalid(extEdges[range.Beg]))
                {
                    return IndexRange.None;
                }
            }
            else if (rangeCount > 1)
            {
                InvokeForExternalEdgesRange(range, edgeInvalid, true, out int degenerateEnd, out _);
                if (degenerateEnd >= 0)
                {
                    if (degenerateEnd == range.End)
                    {
                        return IndexRange.None;
                    }
                    else
                    {
                        range.Beg = extEdges[degenerateEnd].Next;
                    }
                }
                InvokeForExternalEdgesRange(range, edgeInvalid, false, out int degenerateBeg, out _);
                if (degenerateBeg >= 0)
                {
                    if (degenerateBeg == range.Beg)
                    {
                        return IndexRange.None;
                    }
                    else
                    {
                        range.End = extEdges[degenerateBeg].Prev;
                    }
                }
                bool notOppositeEdge = GetFirstExtEdgeInRange(range, edge => !edge.LastPointOpposite, true, out int notOppEdgeIndex);
                if (notOppositeEdge)
                {
                    Log.WriteLine(GetType() + ".TrimExternalEdgesRange: NotOppositeEdge: [" + notOppEdgeIndex + "] " + extEdges[notOppEdgeIndex]);
                    return IndexRange.None;
                }
                rangeCount = range.GetIndexCount();
                if (rangeCount > 2)
                {
                    int beg = range.Beg;
                    int end = range.End;
                    var innerRange = new IndexRange(extEdges[beg].Next, extEdges[end].Prev, range.FullLength);
                    innerDegenerate = GetFirstExtEdgeInRange(innerRange, edge => edge.LastPointDegenerateTriangle, true, out _);
                }
            }
            return range;
        }

        public EdgePeak GetExternalEdgesRangeLoopPeak(int addedPointIndex, IndexRange range)
        {
            int beg = range.Beg;
            int end = range.End;
            int begVertex, endVertex;
            if (beg == end)
            {
                var edge = extEdges[beg];
                begVertex = edge.A;
                endVertex = edge.B;
            }
            else
            {
                int begNextVertex = EdgeEntry.GetSharedVertex(extEdges[beg], extEdges[extEdges[beg].Next]);
                int endPrevVertex = EdgeEntry.GetSharedVertex(extEdges[end], extEdges[extEdges[end].Prev]);
                begVertex = extEdges[beg].GetOtherVertex(begNextVertex);
                endVertex = extEdges[end].GetOtherVertex(endPrevVertex);
            }
            var edgeA = new EdgeEntry(addedPointIndex, begVertex);
            var edgeB = new EdgeEntry(addedPointIndex, endVertex);

            Log.WriteLine(GetType() + ".GetExternalEdgesRangeLoopPeak: " + edgeA + " " + edgeB);
            return new EdgePeak(edgeA, edgeB, points);
        }

        public void LoopCopyExternalEdgesRange(IndexRange range, int addedPointIndex, IncrementalEdgeInfo destEdgeInfo, out EdgePeak loopPeak)
        {
            loopPeak = GetExternalEdgesRangeLoopPeak(addedPointIndex, range);

            var destExtEdges = destEdgeInfo.extEdges;
            int count = range.GetIndexCount();
            int beg = range.Beg;
            int end = range.End;
            if (beg == end)
            {
                var edge = extEdges[beg];
                destExtEdges[0] = loopPeak.EdgeA;
                destExtEdges[1] = edge;
                destExtEdges[2] = loopPeak.EdgeB;
            }
            else
            {
                if (beg > end)
                {
                    int tailLength = extEdgeCount - beg;
                    Array.Copy(extEdges, beg, destExtEdges, 1, tailLength);
                    Array.Copy(extEdges, 0, destExtEdges, 1 + tailLength, end + 1);
                }
                else // if (beg < end)
                {
                    Array.Copy(extEdges, beg, destExtEdges, 1, count);
                }
                destExtEdges[0] = loopPeak.EdgeA;
                destExtEdges[1 + count] = loopPeak.EdgeB;
            }
            destEdgeInfo.extEdgeCount = count + 2;
            destEdgeInfo.RefreshExternalEdgesNextPrev();
            if (beg > end)
            {
                destEdgeInfo.PrintExternalEdges("LoopCopyExternalEdgesRange");
            }
        }

        public bool GetOppositeExternalEdgesRange(int addedPointIndex, int firstExtEdgeIndex, out IndexRange range, out bool pointOnEdge, out bool innerDegenerate)
        {
            int extEdgeIndex = firstExtEdgeIndex;
            if (extEdgeIndex >= 0)
            {
                bool result;
                var point = points[addedPointIndex];

                extEdgeIndex = GetClosestExternalEdge(point, extEdgeIndex);

                range = IndexRange.None;
                innerDegenerate = false;

                bool isPointOpposite = IsPointOppositeToExternalEdge(point, extEdgeIndex, out pointOnEdge);
                if (pointOnEdge)
                {
                    range.Beg = range.End = extEdgeIndex;
                    return false;
                }
                if (isPointOpposite)
                {
                    int beg = GetLastEdgeOppositeToExternalPoint(point, extEdgeIndex, false, out pointOnEdge);
                    if (pointOnEdge)
                    {
                        range.Beg = range.End = beg;
                        return false;
                    }
                    int end = GetLastEdgeOppositeToExternalPoint(point, extEdgeIndex, true, out pointOnEdge);
                    if (pointOnEdge)
                    {
                        range.Beg = range.End = end;
                        return false;
                    }
                    result = GetValidatedExtEdgesRange(point, beg, end, out range, out innerDegenerate);
                }
                else
                {
                    result = GetOppositeExternalEdgesRange(point, extEdgeIndex, out range, out pointOnEdge, out innerDegenerate);
                    if (pointOnEdge)
                    {
                        return false;
                    }
                }
                if (!result && IsLastPointOverSingleExtEdge(out extEdgeIndex, out pointOnEdge))
                {
                    if (!pointOnEdge)
                    {
                        extEdges[extEdgeIndex].ClearLastPointDegenerate();
                    }
                    range = new IndexRange(extEdgeIndex, extEdgeIndex, extEdgeCount);
                    return !pointOnEdge;
                }
                return result;
            }
            else
            {
                throw new ArgumentOutOfRangeException("GetOppositeExternalEdgesRange: firstExtEdgeIndex: " + firstExtEdgeIndex);
            }
        }

        private bool GetOppositeExternalEdgesRange(Vector2 point, int extEdgeIndex, out IndexRange range, out bool pointOnEdge, out bool innerDegenerate)
        {
            int beg = -1;
            int end = -1;
            range = IndexRange.None;
            innerDegenerate = false;

            bool oppEdgeFound = GetFirstEdgesOppositeToExternalPoint(point, extEdgeIndex, out int oppNext, out int oppPrev, out pointOnEdge);
            if (pointOnEdge)
            {
                extEdgeIndex = oppNext >= 0 ? oppNext : oppPrev;
                range.Beg = range.End = extEdgeIndex;
                return false;
            }
            if (oppEdgeFound)
            {
                if (oppNext == oppPrev)
                {
                    beg = end = oppNext;
                }
                else
                {
                    if (oppNext >= 0)
                    {
                        end = GetLastEdgeOppositeToExternalPoint(point, oppNext, true, out pointOnEdge, oppPrev);
                        if (pointOnEdge)
                        {
                            range.Beg = range.End = end;
                            return false;
                        }
                        if (beg < 0)
                        {
                            beg = oppNext;
                        }
                    }
                    if (oppPrev >= 0)
                    {
                        beg = GetLastEdgeOppositeToExternalPoint(point, oppPrev, false, out pointOnEdge, end);
                        if (pointOnEdge)
                        {
                            range.Beg = range.End = beg;
                            return false;
                        }
                        if (end < 0)
                        {
                            end = oppPrev;
                        }
                    }
                }
            }
            return GetValidatedExtEdgesRange(point, beg, end, out range, out innerDegenerate);
        }

        private bool GetValidatedExtEdgesRange(Vector2 point, int beg, int end, out IndexRange range, out bool innerDegenerate)
        {
            innerDegenerate = false;

            if (beg < 0 || end < 0)
            {
                range = IndexRange.None;
                return false;
            }
            else
            {
                bool begEdgeValid = IsTerminalExtEdgeValid(point, beg, false, false);
                bool endEdgeValid = IsTerminalExtEdgeValid(point, end, true, false);

                Log.WriteLine("{0}.GetValidatedExtEdgesRange: | begEdgeValid: {1} | endEdgeValid: {2} | {3} - {4} | {5} - {6}", GetType(), begEdgeValid, endEdgeValid, extEdges[beg].ToShortString(), extEdges[end].ToShortString(), beg, end);
                if (begEdgeValid && endEdgeValid)
                {
                    range = new IndexRange(beg, end, extEdgeCount);
                    var trimmedRange = TrimExternalEdgesRange(range, out innerDegenerate);
                    if (trimmedRange.FullLength > 0 && trimmedRange.GetIndexCount() != range.GetIndexCount())
                    {
                        begEdgeValid = IsTerminalExtEdgeValid(point, trimmedRange.Beg, false, true);
                        endEdgeValid = IsTerminalExtEdgeValid(point, trimmedRange.End, true, true);
                    }
                    Log.WriteLine("{0}.GetValidatedExtEdgesRange: | begEdgeValid: {1} | endEdgeValid: {2} | trimmedRange: {3}", GetType(), begEdgeValid, endEdgeValid, trimmedRange);
                    range = begEdgeValid && endEdgeValid ? trimmedRange : IndexRange.None;
                    return range.FullLength > 0;
                }
                else
                {
                    range = IndexRange.None;
                    return false;
                }
            }
        }

        private int GetLastEdgeOppositeToExternalPoint(Vector2 point, int extEdgeIndex, bool forward, out bool pointOnEdge, int end = -1)
        {
            var getNextEdgeIndex = GetNextExtEdgeIndex(forward);
            int next = getNextEdgeIndex(extEdgeIndex);
            pointOnEdge = false;

            if (end < 0)
            {
                end = extEdgeIndex;
            }
            while (next != end && IsPointOppositeToExternalEdge(point, next, out pointOnEdge))
            {
                if (pointOnEdge)
                {
                    return next;
                }
                next = getNextEdgeIndex(extEdgeIndex = next);
            }
            return extEdgeIndex;
        }

        private int GetNextClosestExternalEdge(Vector2 point, int extEdgeIndex, bool forward, out float minSqrDist)
        {
            var getNextEdgeIndex = GetNextExtEdgeIndex(forward);
            var firstEdge = extEdges[extEdgeIndex];
            float sqrDist = Vector2.DistanceSquared(point, firstEdge.GetMidPoint(points));
            minSqrDist = float.MaxValue;

            int nextEdgeIndex = extEdgeIndex;
            int counter = 0;
            while (sqrDist < minSqrDist && counter < extEdgeCount)
            {
                //Log.WriteLine(GetType() + ".GetNextClosestExternalEdge: " + extEdges[extEdgeIndex] + " -> " + extEdges[nextEdgeIndex]);
                extEdgeIndex = nextEdgeIndex;
                minSqrDist = sqrDist;
                nextEdgeIndex = getNextEdgeIndex(nextEdgeIndex);
                var nextEdge = extEdges[nextEdgeIndex];
                sqrDist = Vector2.DistanceSquared(point, nextEdge.GetMidPoint(points));
            }
            return extEdgeIndex;
        }

        private int GetClosestExternalEdge(Vector2 point, int extEdgeIndex)
        {
            int next = GetNextClosestExternalEdge(point, extEdgeIndex, true, out float nextSqrDist);
            int prev = GetNextClosestExternalEdge(point, extEdgeIndex, false, out float prevSqrDist);
            extEdgeIndex = nextSqrDist < prevSqrDist ? next : prev;
            //Log.WriteLine(GetType() + ".GetClosestExternalEdge: " + extEdges[next] + " OR " + extEdges[prev]);
            Log.WriteLine("{0}.GetClosestExternalEdge: [{1}] {2}", GetType(), extEdgeIndex, extEdges[extEdgeIndex]);
            return extEdgeIndex;
        }

        private EdgeEntry GetPrevExtEdge(int edgeIndex)
        {
            int prev = extEdges[edgeIndex].Prev;
            return extEdges[prev];
        }

        private EdgeEntry GetNextExtEdge(int edgeIndex)
        {
            int next = extEdges[edgeIndex].Next;
            return extEdges[next];
        }

        private Func<int, int> GetNextExtEdgeIndex(bool forward)
        {
            if (forward)
            {
                return edgeIndex => extEdges[edgeIndex].Next;
            }
            else
            {
                return edgeIndex => extEdges[edgeIndex].Prev;
            }
        }

        private bool GetFirstEdgesOppositeToExternalPoint(Vector2 point, int extEdgeIndex, out int oppNext, out int oppPrev, out bool pointOnEdge)
        {
            var getNextEdgeIndex = GetNextExtEdgeIndex(true);
            var getPrevEdgeIndex = GetNextExtEdgeIndex(false);
            int nextEdgeIndex = extEdgeIndex;
            int prevEdgeIndex = extEdgeIndex;
            pointOnEdge = false;

            int counter = 0;
            while (counter < extEdgeCount)
            {
                nextEdgeIndex = getNextEdgeIndex(nextEdgeIndex);
                prevEdgeIndex = getPrevEdgeIndex(prevEdgeIndex);
                bool oppositeToNext = IsPointOppositeToExternalEdge(point, nextEdgeIndex, out pointOnEdge);
                if (pointOnEdge)
                {
                    oppPrev = -1;
                    oppNext = nextEdgeIndex;
                    return false;
                }
                bool oppositeToPrev = IsPointOppositeToExternalEdge(point, prevEdgeIndex, out pointOnEdge);
                if (pointOnEdge)
                {
                    oppPrev = prevEdgeIndex;
                    oppNext = -1;
                    return false;
                }
                if (oppositeToNext || oppositeToPrev)
                {
                    oppNext = oppositeToNext ? nextEdgeIndex : -1;
                    oppPrev = oppositeToPrev ? prevEdgeIndex : -1;
                    return true;
                }
                counter++;
            }
            Log.WriteLine(GetType() + "GetFirstEdgesOppositeToExternalPoint: OPPOSITE EDGE NOT FOUND " + counter);
            oppNext = oppPrev = -1;
            return false;
        }

        private bool IsPointOppositeToExternalEdge(Vector2 point, int edgeIndex, out bool pointOnEdge, string logPrefix = null)
        {
            return IsPointOppositeToExternalEdge(point, extEdges[edgeIndex], edgeIndex, out pointOnEdge, logPrefix);
        }

        private bool IsPointOppositeToExternalEdge(Vector2 point, EdgeEntry extEdge, int edgeIndex, out bool pointOnEdge, string logPrefix = null)
        {
            var extTriangle = GetExternalTriangle(extEdge);

            var edge = extEdge.GetVector(points);
            var oppVert = extTriangle.GetOppositeVertex(extEdge, points, out _);
            var edgeVertA = points[extEdge.A];
            int sign1 = Mathv.GetAngleSign(point - edgeVertA, edge);
            int sign2 = Mathv.GetAngleSign(oppVert - edgeVertA, edge);
            if (sign2 == 0)
            {
                throw new Exception("IsPointOppositeToExternalEdge: signs: " + sign1 + ", " + sign2 + " for edge: " + extEdge);
            }
            bool opposite = sign1 != 0 && sign1 != sign2;
            bool setLastPointData = edgeIndex >= 0;
            extEdge.LastPointOpposite = opposite;

            pointOnEdge = extEdge.IsPointOnEdge(point, points, setLastPointData, out bool inRange) || (sign1 == 0 && inRange);
            if (setLastPointData)
            {
                extEdges[edgeIndex].SetLastPointData(extEdge);
                lastPointExtEdgeIndices.Add(edgeIndex);
            }
            logPrefix = string.IsNullOrEmpty(logPrefix) ? GetType().Name : string.Format("{0}.{1}", logPrefix, GetType().Name);
            Log.WriteLine("{0}.IsPointOppositeToExternalEdge: [{1}] {2} | pointOnEdge: {3}", logPrefix, edgeIndex, extEdge.ToLastPointDataString(false), pointOnEdge);
            return opposite;
        }

        private bool IsTerminalExtEdgeValid(Vector2 addedPoint, int extEdgeIndex, bool nextForward, bool trimmed)
        {
            var extEdge = extEdges[extEdgeIndex];
            int next = nextForward ? extEdge.Next : extEdge.Prev;
            var nextEdge = extEdges[next];
            var edgePeak = new EdgePeak(extEdge, nextEdge, points);
            if (trimmed)
            {
                float angleB = edgePeak.GetPointRayAngleB(addedPoint, points);
                Log.WriteLine(GetType() + ".IsTerminalExtEdgeValid: angle between: " + edgePeak.EdgeB + " and pointRay from (" + edgePeak.PeakVertex + "): " + angleB.ToStringF2());
                return angleB > 150f;
            }
            else
            {
                var pointRay = addedPoint - points[edgePeak.PeakVertex];
                int sign1 = edgePeak.AngleSign;
                int sign2 = Mathv.GetAngleSign(edgePeak.EdgeVecB, pointRay);
                if (sign2 == 0)
                {
                    float rayDotEdgeB = Vector2.Dot(edgePeak.EdgeVecB, pointRay);
                    Log.WriteLine(GetType() + ".IsTerminalExtEdgeValid: signs: (" + sign1 + ", " + sign2 + ") for edges: " + extEdge + " " + nextEdge + " rayDotEdgeB: " + rayDotEdgeB.ToStringF2());
                    return rayDotEdgeB < 0f;
                }
                else
                {
                    Log.WriteLine(GetType() + ".IsTerminalExtEdgeValid: signs: (" + sign1 + ", " + sign2 + ") for edges: " + extEdge + " " + nextEdge);
                    return sign1 == sign2;
                }
            }
        }

        private bool IsLastPointOverSingleExtEdge(out int extEdgeIndex, out bool pointOnEdge)
        {
            int oppositeInRangeCount = 0;
            pointOnEdge = false;
            extEdgeIndex = -1;

            for (int i = 0; i < lastPointExtEdgeIndices.Count; i++)
            {
                int edgeIndex = lastPointExtEdgeIndices[i];
                var extEdge = extEdges[edgeIndex];
                if (extEdge.LastPointOpposite && extEdge.LastPointInRange)
                {
                    oppositeInRangeCount++;
                    extEdgeIndex = edgeIndex;
                }
            }
            if (oppositeInRangeCount == 1 && extEdgeIndex >= 0)
            {
                var extEdge = extEdges[extEdgeIndex];
                Log.WriteLine(GetType() + ".IsLastPointOverSingleExtEdge: " + extEdge.ToLastPointDataString(true));
                //bool overEdge = extEdge.LastPointDegenerateTriangle || extEdge.LastPointDegenerateAngle;
                //extEdgeIndex = overEdge ? extEdgeIndex : -1;
                //return overEdge;
                return true;
            }
            else
            {
                Log.WriteLine(GetType() + ".IsLastPointOverSingleExtEdge: FALSE | oppositeInRangeCount: " + oppositeInRangeCount);
                extEdgeIndex = -1;
            }
            return false;
        }

        protected void ForEachInternalEdge(Triangle triangle, Action<int, EdgeEntry> action)
        {
            ForEachEdge(triangle, (edgeKey, edge) => {
                if (IsEdgeInternal(edgeKey))
                {
                    action(edgeKey, edge);
                }
            });
        }

        protected void ForEachExternalEdge(Triangle triangle, Action<int, EdgeEntry> action)
        {
            ForEachEdge(triangle, (edgeKey, edge) => {
                if (IsEdgeExternal(edgeKey))
                {
                    action(edgeKey, edge);
                }
            });
        }

        protected void RefreshExternalEdgesNextPrev(int startIndex = 0)
        {
            EdgeEntry.RefreshSortedEdgesNextPrev(extEdges, extEdgeCount, startIndex);
        }

        protected bool GetExternalEdgesWithVertex(int vertex, out int prev, out int next)
        {
            int edgeIndex = GetExternalEdgeIndexWithPredicate(i => extEdges[i].HasVertex(vertex));
            if (edgeIndex < 0)
            {
                throw new Exception("GetExternalEdgesWithVertex: Edge Not Found: vertex: " + vertex);
            }
            prev = extEdges[edgeIndex].Prev;
            next = extEdges[edgeIndex].Next;
            if (extEdges[next].HasVertex(vertex))
            {
                prev = edgeIndex;
                return true;
            }
            else if (extEdges[prev].HasVertex(vertex))
            {
                next = edgeIndex;
                return true;
            }
            else
            {
                throw new Exception("GetExternalEdgesWithVertex: " + vertex);
            }
        }

        private int GetExternalEdgeIndex(EdgeEntry edge, int startIndex = -1)
        {
            int edgeIndex = GetExternalEdgeIndexWithPredicate(i => extEdges[i].Equals(edge), startIndex);
            //Log.WriteLine(GetType() + ".GetExternalEdgeIndex: extEdgeCount: " + extEdgeCount + " edge: " + edge + " edgeIndex: " + edgeIndex);
            if (edgeIndex < 0)
            {
                throw new Exception("GetExternalEdgeIndex: Edge Not Found: " + edge + " startIndex: " + startIndex);
            }
            return edgeIndex;
        }

        private int GetExternalEdgeIndexWithPredicate(Predicate<int> predicate, int startIndex = -1)
        {
            int edgeIndex = -1;
            if (startIndex >= 0 && startIndex < extEdgeCount)
            {
                if (predicate(startIndex))
                {
                    edgeIndex = startIndex;
                }
                else
                {
                    bool result = false;
                    bool nextResult = false;
                    int next = extEdges[startIndex].Next;
                    int prev = extEdges[startIndex].Prev;
                    while (!result && next != prev)
                    {
                        //Log.WriteLine(GetType() + ".GetExternalEdgeIndex: " + next + " " + prev);
                        nextResult = predicate(next);
                        result = nextResult || predicate(prev);
                        next = extEdges[next].Next;
                        prev = extEdges[prev].Prev;
                    }
                    if (result)
                    {
                        edgeIndex = nextResult ? next : prev;
                    }
                }
            }
            else
            {
                int halfEdgeCount = extEdgeCount >> 1;
                int fromEndIndex;
                for (int i = 0; i <= halfEdgeCount; i++)
                {
                    if (predicate(i))
                    {
                        edgeIndex = i;
                        break;
                    }
                    else if (predicate(fromEndIndex = extEdgeCount - 1 - i))
                    {
                        edgeIndex = fromEndIndex;
                        break;
                    }
                }
            }
            return edgeIndex;
        }

        private void RemoveInnerEdgeTriangleFromDict(int edgeKey, long triangleKey)
        {
            if (innerEdgeTriangleDict.ContainsKey(edgeKey))
            {
                var innerEdgeData = innerEdgeTriangleDict[edgeKey];
                innerEdgeData.ClearTriangleKey(triangleKey);
                if (innerEdgeData.IsClear)
                {
                    innerEdgeTriangleDict.Remove(edgeKey);
                }
                else
                {
                    innerEdgeTriangleDict[edgeKey] = innerEdgeData;
                }
            }
        }

        private void AddInnerEdgeToTriangleDict(EdgeEntry edge, int edgeKey, long triangleKey, out long extTriangleKey)
        {
            if (innerEdgeTriangleDict.ContainsKey(edgeKey))
            {
                var edgeData = innerEdgeTriangleDict[edgeKey];
                edgeData.SetTriangleKey(triangleKey);
                innerEdgeTriangleDict[edgeKey] = edgeData;
                extTriangleKey = -1;
            }
            else
            {
                if (extEdgeTriangleDict.TryGetValue(edgeKey, out extTriangleKey))
                {
                    innerEdgeTriangleDict.Add(edgeKey, new InnerEdgeData(edge, triangleKey, extTriangleKey));
                    extEdgeTriangleDict.Remove(edgeKey);
                }
                else
                {
                    innerEdgeTriangleDict.Add(edgeKey, new InnerEdgeData(edge, triangleKey));
                    extTriangleKey = -1;
                }
            }
        }

        private void SetEdgePointsExternal(EdgeEntry edge, bool external)
        {
            SetPointExternal(edge.A, external);
            SetPointExternal(edge.B, external);
        }

        private void SetTrianglePointsExternal(Triangle triangle, bool external)
        {
            triangle.GetIndices(indexBuffer);
            for (int i = 0; i < 3; i++)
            {
                SetPointExternal(indexBuffer[i], external);
            }
        }

        public EdgeEntry GetExternalEdge(int edgeIndex)
        {
            return extEdges[edgeIndex];
        }

        public int GetExternalEdgeKey(int edgeIndex)
        {
            return GetEdgeKey(extEdges[edgeIndex]);
        }

        private bool TryGetExternalTriangleKey(EdgeEntry extEdge, out long triangleKey, out int edgeKey)
        {
            edgeKey = GetEdgeKey(extEdge);
            return extEdgeTriangleDict.TryGetValue(edgeKey, out triangleKey);
        }

        private Triangle GetExternalTriangle(EdgeEntry extEdge)
        {
            if (TryGetExternalTriangleKey(extEdge, out long triangleKey, out int edgeKey))
            {
                return GetTriangleRef(triangleKey, out _);
            }
            else
            {
                throw new KeyNotFoundException(string.Format("GetExternalTriangle: edgeKey {0} not present in extEdgeTriangleDict", edgeKey));
            }
        }

        private Triangle GetExternalTriangle(int extEdgeIndex)
        {
            if (extEdgeIndex >= 0 && extEdgeIndex < extEdgeCount)
            {
                return GetExternalTriangle(extEdges[extEdgeIndex]);
            }
            throw new Exception("GetExternalTriangle: " + extEdgeIndex + " out of bounds: 0 - " + extEdgeCount);
        }

        protected ref Triangle GetTriangleRef(long triangleKey, out int triangleIndex)
        {
            return ref triangleSet.GetTriangleRef(triangleKey, out triangleIndex);
        }
    }
}
