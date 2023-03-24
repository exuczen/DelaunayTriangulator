using System;
using System.Collections.Generic;

namespace Triangulation
{
    public class EdgeInfo
    {
        public int ExtEdgeCount => extEdgeCount;
        public EdgeEntry[] ExtEdges => extEdges;
        public EdgeEntry[] AddedExtEdges => addedExtEdges;
        public Dictionary<int, int> EdgeCounterDict => edgeCounterDict;
        public int InnerEdgeCount => innerEdgeTriangleDict.Count;

        protected readonly IExceptionThrower exceptionThrower = null;

        protected readonly Dictionary<int, int> edgeCounterDict = new Dictionary<int, int>();
        protected readonly Dictionary<int, long> extEdgeTriangleDict = new Dictionary<int, long>();
        protected readonly Dictionary<int, InnerEdgeData> innerEdgeTriangleDict = new Dictionary<int, InnerEdgeData>();
        protected readonly EdgeEntry[] edgeBuffer = new EdgeEntry[3];
        protected readonly EdgeEntry[] extEdges = null;
        protected readonly EdgeEntry[] addedExtEdges = null;
        protected readonly int[] edgeKeyBuffer = new int[3];
        protected readonly int[] indexBuffer = new int[3];
        protected readonly long[] triangleKeyBuffer = new long[3];

        protected readonly TriangleSet triangleSet = null;
        protected readonly Vector2[] points = null;
        protected readonly bool[] pointsExternal = null;

        protected readonly List<IndexRange> extEdgeRanges = new List<IndexRange>();
        protected readonly List<int> edgeIndexPool = new List<int>();

        protected readonly List<int> lastPointExtEdgeIndices = new List<int>();

        protected int extEdgeCount = 0;

        public EdgeInfo(TriangleSet triangleSet, Vector2[] points, IExceptionThrower exceptionThrower) : this(points, exceptionThrower)
        {
            this.triangleSet = triangleSet;
        }

        public EdgeInfo(Vector2[] points, IExceptionThrower exceptionThrower)
        {
            extEdges = new EdgeEntry[points.Length << 2];
            addedExtEdges = new EdgeEntry[1024];

            this.points = points;
            this.exceptionThrower = exceptionThrower;

            pointsExternal = new bool[points.Length];
        }

        #region Logs
        public void PrintExtEdgeTriangleDict(string prefix = null)
        {
            PrintEdgeDict(extEdgeTriangleDict, ".PrintExtEdgeTriangleDict: " + extEdgeTriangleDict.Count + " " + prefix);
        }

        public void PrintInnerEdgeTriangleDict(string prefix = null)
        {
            PrintEdgeDict(innerEdgeTriangleDict, ".PrintInnerEdgeTriangleDict: " + innerEdgeTriangleDict.Count + " " + prefix);
        }

        public void PrintEdgeCounterDict(string prefix = null)
        {
            PrintEdgeDict(edgeCounterDict, ".PrintEdgeCounterDict: " + edgeCounterDict.Count + " " + prefix);
        }

        private void PrintEdgeDict<T>(Dictionary<int, T> dict, string prefix)
        {
            Log.WriteLine(GetType() + prefix);
            foreach (var kvp in dict)
            {
                GetEdgeFromKey(kvp.Key, out int edgeA, out int edgeB);
                Log.WriteLine(GetType() + ".PrintEdgeDict: edge: " + edgeA + " " + edgeB + " value: " + kvp.Value);
            }
        }

        public void PrintExternalEdges(string prefix = null)
        {
            Log.PrintEdges(extEdges, extEdgeCount, "PrintExternalEdges: " + prefix);
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

        public bool ForEachExternalEdge(Func<EdgeEntry, bool> action)
        {
            for (int i = 0; i < extEdgeCount; i++)
            {
                if (!action(extEdges[i]))
                {
                    return false;
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

        public void SetPointExternal(int pointIndex, bool external)
        {
            //Log.WriteLine(GetType() + ".SetPointExternal: " + pointIndex + " " + external);
            pointsExternal[pointIndex] = external;
        }

        public bool AddEdgesToCounterDict(Triangle triangle)
        {
            triangle.GetEdges(edgeBuffer);
            for (int i = 0; i < 3; i++)
            {
                if (IsEdgeInternal(edgeBuffer[i], out int edgeKey))
                {
                    string message = GetType() + ".AddEdgesToCounterDict: IsEdgeInternal: " + edgeBuffer[i];
                    Log.WriteError(message);
                    exceptionThrower.ThrowException(message, ErrorCode.InternalEdgeExists);
                    return false;
                }
                edgeKeyBuffer[i] = edgeKey;
            }
            for (int i = 0; i < 3; i++)
            {
                int edgeKey = edgeKeyBuffer[i];
                if (edgeCounterDict.ContainsKey(edgeKey))
                {
                    edgeCounterDict[edgeKey]++;
                }
                else
                {
                    edgeCounterDict.Add(edgeKey, 1);
                }
            }
            //Log.WriteLine("AddEdgesToCounterDict: " + triangle);
            //PrintEdgeCounterDict("AddEdgesToCounterDict: ");
            return true;
        }

        public void RemoveEdgesFromCounterDict(Triangle triangle)
        {
            triangle.GetIndices(indexBuffer);
            ForEachEdge(triangle, (edgeKey, edge) => {
                if (edgeCounterDict.ContainsKey(edgeKey))
                {
                    if (--edgeCounterDict[edgeKey] <= 0)
                    {
                        edgeCounterDict.Remove(edgeKey);
                    }
                }
            });
            //Log.WriteLine("RemoveEdgesFromCounterDict: " + triangle);
            //PrintEdgeCounterDict("RemoveEdgesFromCounterDict: ");
        }

        public void RemoveEdgesFromDicts(Triangle triangle)
        {
            triangle.GetIndices(indexBuffer);
            ForEachEdge(triangle, (edgeKey, edge) => {
                if (edgeCounterDict.ContainsKey(edgeKey))
                {
                    int edgeCount = edgeCounterDict[edgeKey];
                    if (edgeCount == 2)
                    {
                        RemoveInnerEdgeTriangleFromDict(edgeKey, triangle.Key);
                    }
                    edgeCount = --edgeCounterDict[edgeKey];
                    if (edgeCount <= 0)
                    {
                        edgeCounterDict.Remove(edgeKey);
                        extEdgeTriangleDict.Remove(edgeKey);
                        innerEdgeTriangleDict.Remove(edgeKey);
                    }
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

        public bool GetFirstExternalEdge(Triangle triangle, out EdgeEntry externalEdge)
        {
            return GetFirstEdgeWithPredicate(triangle, IsEdgeExternal, out externalEdge, out _);
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

        public IndexRange GetPeakExternalEdgesRange(EdgePeak peak, EdgeInfo baseEdgeInfo, List<int> pointsToClear)
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

        public void ReplacePeakExternalEdges(IndexRange addedPeakRange, EdgeInfo addedEdgeInfo)
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
            pointsToClear.Add(peak.PeakVertex);

            if (triangleSet.Count > 1)
            {
                var peakRange = GetPeakExternalEdgesRange(peak);

                var oppEdge = peak.GetOppositeEdge(out _);
                addedExtEdges[0] = oppEdge;

                InsertExternalEdges(addedExtEdges, 1, peakRange, true);
            }
            else
            {
                pointsToClear.Add(peak.VertexA);
                pointsToClear.Add(peak.VertexB);

                extEdgeCount = 0;
            }
        }

        public void FindExternalEdges(int pointsCount)
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
            JoinSortExternalEdges(exceptionThrower);
        }

        public void UpdateTriangleDicts(EdgeInfo addedEdgeInfo)
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
                        triangle.FillColor = Color.LightGreen;

                        Log.WriteLine(GetType() + ".UpdateTriangleDicts: " + edge + " changed from internal to external");
                    }
                    else
                    {
                        throw new Exception("UpdateTriangleDicts: !IsEdgeExternal: " + edge);
                    }
                }
            });
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

        public void AddEdgesToTriangleDicts(ref Triangle triangle, Color innerColor)
        {
            if (triangle.Key < 0)
            {
                throw new Exception("AddEdgesToTriangleDicts: " + triangle.Key);
            }
            long triangleKey = triangle.Key;
            int extTriangleCount = 0;
            int extEdgeCount = 0;

            ForEachEdge(triangle, (edgeKey, edge) => {
                if (edgeCounterDict.TryGetValue(edgeKey, out int edgeCount))
                {
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
                }
            });
            triangle.FillColor = extEdgeCount > 0 ? Color.LightGreen : innerColor;
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

        public void AddEdgesToCounterDict(Triangle[] triangles, int trianglesCount)
        {
            //PrintEdgeCounterDict("I");
            for (int i = trianglesCount - 1; i >= 0; i--)
            {
                AddEdgesToCounterDict(triangles[i]);

                //if (!AddEdgesToCounterDict(triangles[i]))
                //{
                //    Log.WriteLine(GetType() + ".AddEdgesToCounterDict: REMOVED TRIANGLE: " + triangles[i]);
                //    triangles[i] = triangles[--trianglesCount];
                //}
                //else
                //{
                //    Log.WriteLine(GetType() + ".AddEdgesToCounterDict: " + triangles[i]);
                //}
            }
            //PrintEdgeCounterDict("II");
        }

        public void JoinSortExternalEdges(IExceptionThrower exceptionThrower)
        {
            extEdgeRanges.Clear();
            edgeIndexPool.Clear();

            if (extEdgeCount == 0)
            {
                return;
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

                if (iNextNegative || iPrevNegative)
                {
                    if (iNextNegative && !iPrevNegative)
                    {
                        edgeI.SwapNextPrev();
                    }
                    //Log.WriteLine(GetType() + ".JoinSortExternalEdges: terminal edge: " + edgeI);
                    edgeIndexPool.Insert(0, i);
                }
                else
                {
                    edgeIndexPool.Add(i);
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
                    edgeIndexPool.Remove(edgeIndex);
                    var extEdge = extEdges[edgeIndex];

                    int nextIndex = extEdge.Next;

                    range.End = edgeCounter;

                    extEdge.Prev = extEdge.Prev >= 0 ? edgeCounter - 1 : -1;

                    if (nextIndex == firstIndex || nextIndex < 0)
                    {
                        //Log.WriteLine(GetType() + ".JoinSortExternalEdges: ADD RANGE: " + range + " edgeCounter: " + edgeCounter + " " + extEdge);
                        extEdgeRanges.Add(range);

                        if (edgeIndexPool.Count > 0)
                        {
                            nextIndex = firstIndex = edgeIndexPool[0];
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

            string errorMessage = null;
            void printExternalEdges()
            {
                PrintExternalEdges("JoinSortExternalEdges 2");
                Log.PrintIndexRanges(extEdgeRanges);
            }
            if (extEdgeRanges.Count > 1)
            {
                errorMessage = "JoinSortExternalEdges: RANGES COUNT: " + extEdgeRanges.Count;
            }
            else if (!extEdges[0].SharesVertex(extEdges[extEdgeCount - 1]))
            {
                errorMessage = "JoinSortExternalEdges: OPEN LOOP";
            }
            if (!string.IsNullOrEmpty(errorMessage))
            {
                Log.WriteError(errorMessage);
                printExternalEdges();
                exceptionThrower.ThrowException(new NotImplementedException(errorMessage));
            }
            //printExternalEdges();
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

        public bool InvokeForExternalEdgesRange(IndexRange range, Predicate<EdgeEntry> action, bool forward, out int lastIndex)
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
            lastIndex = -1;

            while (edgeIndex != end)
            {
                //Log.WriteLine(GetType() + ".InvokeForExternalEdgesRange: " + edgeIndex + " " + extEdges[edgeIndex].LastPointDegenerateAngle);
                if (!action(extEdges[edgeIndex]))
                {
                    return false;
                }
                lastIndex = edgeIndex;
                edgeIndex = getNextEdgeIndex(edgeIndex);
            }
            if (!action(extEdges[end]))
            {
                return false;
            }
            lastIndex = end;
            return true;
        }

        public IndexRange TrimExternalEdgesRange(IndexRange range, out bool innerDegenerate)
        {
            innerDegenerate = false;

            int rangeCount = range.GetIndexCount();
            if (rangeCount == 1)
            {
                if (extEdges[range.Beg].LastPointDegenerateAngle)
                {
                    return IndexRange.None;
                }
            }
            else if (rangeCount > 1)
            {
                InvokeForExternalEdgesRange(range, edge => edge.LastPointDegenerateAngle, true, out int degenerateEnd);
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
                InvokeForExternalEdgesRange(range, edge => edge.LastPointDegenerateAngle, false, out int degenerateBeg);
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
                rangeCount = range.GetIndexCount();
                if (rangeCount > 2)
                {
                    int beg = range.Beg;
                    int end = range.End;
                    var innerRange = new IndexRange(extEdges[beg].Next, extEdges[end].Prev, range.FullLength);
                    innerDegenerate = !InvokeForExternalEdgesRange(innerRange, edge => !edge.LastPointDegenerateTriangle, true, out _);
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

        public void LoopCopyExternalEdgesRange(IndexRange range, int addedPointIndex, EdgeInfo destEdgeInfo, out EdgePeak loopPeak)
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

        public bool GetOppositeExternalEdgesRange(int addedPointIndex, EdgeEntry firstExtEdge, out IndexRange range, out bool pointOnEdge)
        {
            int extEdgeIndex = GetExternalEdgeIndex(firstExtEdge);
            if (extEdgeIndex >= 0)
            {
                var point = points[addedPointIndex];

                extEdgeIndex = GetClosestExternalEdge(point, extEdgeIndex);

                range = IndexRange.None;

                bool isPointOpposite = IsPointOppositeToExternalEdge(point, extEdgeIndex, out pointOnEdge);
                if (pointOnEdge)
                {
                    range.Beg = range.End = extEdgeIndex;
                    return false;
                }
                if (isPointOpposite)
                {
                    int beg = GetLastEdgeOppositeToExternalPoint(point, extEdgeIndex, false, true, out pointOnEdge);
                    if (pointOnEdge)
                    {
                        range.Beg = range.End = beg;
                        return false;
                    }
                    int end = GetLastEdgeOppositeToExternalPoint(point, extEdgeIndex, true, true, out pointOnEdge);
                    if (pointOnEdge)
                    {
                        range.Beg = range.End = end;
                        return false;
                    }
                    bool begEdgeValid = IsTerminalExtEdgeValid(point, beg, false);
                    bool endEdgeValid = IsTerminalExtEdgeValid(point, end, true);

                    range = new IndexRange(beg, end, extEdgeCount);
                    bool edgesValid = extEdgeIndex >= 0 && begEdgeValid && endEdgeValid;
                    return edgesValid && !pointOnEdge;
                }
                else
                {
                    bool result = GetOppositeExternalEdgesRange(point, extEdgeIndex, true, out range, out pointOnEdge);
                    if (pointOnEdge)
                    {
                        return false;
                    }
                    else if (!result)
                    {
                        result = GetOppositeExternalEdgesRange(point, extEdgeIndex, false, out range, out pointOnEdge);
                    }
                    return result;
                }
            }
            else
            {
                throw new Exception("!GetFirstExternalEdge");
            }
        }

        private bool GetOppositeExternalEdgesRange(Vector2 point, int extEdgeIndex, bool forward, out IndexRange range, out bool pointOnEdge)
        {
            int beg, end;
            bool begEdgeValid = true, endEdgeValid = true;
            range = IndexRange.None;

            extEdgeIndex = GetFirstEdgeOppositeToExternalPoint(point, extEdgeIndex, forward, out bool isPointOppositeToNext, out pointOnEdge);
            if (pointOnEdge)
            {
                range.Beg = range.End = extEdgeIndex;
                return false;
            }
            if (extEdgeIndex >= 0)
            {
                if (!forward)
                {
                    isPointOppositeToNext = !isPointOppositeToNext;
                }
                end = GetLastEdgeOppositeToExternalPoint(point, extEdgeIndex, isPointOppositeToNext, true, out pointOnEdge);
                if (pointOnEdge)
                {
                    range.Beg = range.End = end;
                    return false;
                }
                if (isPointOppositeToNext)
                {
                    beg = extEdgeIndex;
                }
                else
                {
                    beg = end;
                    end = extEdgeIndex;
                }
                begEdgeValid = IsTerminalExtEdgeValid(point, beg, false);
                endEdgeValid = IsTerminalExtEdgeValid(point, end, true);
            }
            else
            {
                beg = end = -1;
            }
            Log.WriteLine(GetType() + ".GetOppositeExternalEdgesRange: begEdgeValid: " + begEdgeValid + " endEdgeValid: " + endEdgeValid);

            range = new IndexRange(beg, end, extEdgeCount);
            bool edgesValid = extEdgeIndex >= 0 && begEdgeValid && endEdgeValid;
            return edgesValid && !pointOnEdge;
        }

        private int GetLastEdgeOppositeToExternalPoint(Vector2 point, int extEdgeIndex, bool forward, bool skipFirstCheck, out bool pointOnEdge)
        {
            var getNextEdgeIndex = GetNextExtEdgeIndex(forward);
            int next = extEdgeIndex;
            pointOnEdge = false;

            while (skipFirstCheck || IsPointOppositeToExternalEdge(point, next, out pointOnEdge))
            {
                if (pointOnEdge)
                {
                    return next;
                }
                next = getNextEdgeIndex(extEdgeIndex = next);
                skipFirstCheck = false;
            }
            return extEdgeIndex;
        }

        private int GetNextClosestExternalEdge(Vector2 point, int extEdgeIndex, bool forward, out float minSqrDist)
        {
            var getNextEdgeIndex = GetNextExtEdgeIndex(forward);
            var firstEdge = extEdges[extEdgeIndex];
            float sqrDist = Vector2.SqrDistance(point, firstEdge.GetMidPoint(points));
            minSqrDist = float.MaxValue;

            int nextEdgeIndex = extEdgeIndex;
            int counter = 0;
            while (sqrDist < minSqrDist && counter < extEdgeCount)
            {
                extEdgeIndex = nextEdgeIndex;
                minSqrDist = sqrDist;
                nextEdgeIndex = getNextEdgeIndex(nextEdgeIndex);
                var nextEdge = extEdges[nextEdgeIndex];
                sqrDist = Vector2.SqrDistance(point, nextEdge.GetMidPoint(points));
            }
            return extEdgeIndex;
        }

        private int GetClosestExternalEdge(Vector2 point, int extEdgeIndex)
        {
            int next = GetNextClosestExternalEdge(point, extEdgeIndex, true, out float nextSqrDist);
            int prev = GetNextClosestExternalEdge(point, extEdgeIndex, false, out float prevSqrDist);
            extEdgeIndex = nextSqrDist < prevSqrDist ? next : prev;
            Log.WriteLine(GetType() + ".GetClosestExternalEdge: " + extEdges[extEdgeIndex] + " " + extEdgeIndex);
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

        private int GetFirstEdgeOppositeToExternalPoint(Vector2 point, int extEdgeIndex, bool forward, out bool isPointOppositeToNext, out bool pointOnEdge)
        {
            var getNextEdgeIndex = GetNextExtEdgeIndex(forward);
            var getPrevEdgeIndex = GetNextExtEdgeIndex(!forward);
            int nextEdgeIndex = extEdgeIndex;
            int prevEdgeIndex = extEdgeIndex;
            bool isPointOpposite = false;
            isPointOppositeToNext = false;
            pointOnEdge = false;

            int counter = 0;
            while (!isPointOpposite && counter < extEdgeCount)
            {
                nextEdgeIndex = getNextEdgeIndex(nextEdgeIndex);
                prevEdgeIndex = getPrevEdgeIndex(prevEdgeIndex);
                isPointOppositeToNext = IsPointOppositeToExternalEdge(point, nextEdgeIndex, out pointOnEdge);
                if (pointOnEdge)
                {
                    return nextEdgeIndex;
                }
                isPointOpposite = isPointOppositeToNext || IsPointOppositeToExternalEdge(point, prevEdgeIndex, out pointOnEdge);
                if (!isPointOppositeToNext && pointOnEdge)
                {
                    return prevEdgeIndex;
                }
                counter++;
            }
            if (!isPointOpposite)
            {
                Log.WriteLine(GetType() + "GetFirstEdgeOppositeToExternalPoint: OPPOSITE EDGE NOT FOUND " + counter);
                return -1;
            }
            else
            {
                return isPointOppositeToNext ? nextEdgeIndex : prevEdgeIndex;
            }
        }

        private bool IsPointOppositeToExternalEdge(Vector2 point, int edgeIndex, out bool pointOnEdge)
        {
            var extEdge = extEdges[edgeIndex];
            var extTriangle = GetExternalTriangle(edgeIndex);

            var edge = extEdge.GetVector(points);
            var oppVert = extTriangle.GetOppositeVertex(extEdge, points, out _);
            var edgeVertA = points[extEdge.A];
            int sign1 = MathF.Sign(Vector2.Cross(point - edgeVertA, edge));
            int sign2 = MathF.Sign(Vector2.Cross(oppVert - edgeVertA, edge));
            if (sign2 == 0)
            {
                throw new Exception("IsPointOppositeToExternalEdge: signs: " + sign1 + ", " + sign2 + " for edge: " + extEdge);
            }
            bool opposite = sign1 != 0 && sign1 != sign2;

            pointOnEdge = extEdge.SetLastPointOnEgdeData(point, points, out bool inRange) || (sign1 == 0 && inRange);
            extEdges[edgeIndex] = extEdge;

            lastPointExtEdgeIndices.Add(edgeIndex);

            Log.WriteLine(GetType() + ".IsPointOppositeToExternalEdge: edge: " + edgeIndex + " " + extEdge + " pointOnEdge: " + pointOnEdge + " opposite: " + opposite);
            return opposite;
        }

        private bool IsTerminalExtEdgeValid(Vector2 addedPoint, int extEdgeIndex, bool nextForward)
        {
            var extEdge = extEdges[extEdgeIndex];
            int next = nextForward ? extEdge.Next : extEdge.Prev;
            var nextEdge = extEdges[next];
            int sharedIndex = EdgeEntry.GetSharedVertex(extEdge, nextEdge);
            var pointRay = addedPoint - points[sharedIndex];
            var edgePeak = new EdgePeak(extEdge, nextEdge, points);
            int sign1 = edgePeak.AngleSign;
            int sign2 = MathF.Sign(Vector2.Cross(edgePeak.EdgeVecB, pointRay));
            //float crossNextEdgePointRay = Vector2.Cross(edgePeak.EdgeVecB.Normalize(), pointRay.Normalize());
            //int sign2 = MathF.Abs(crossNextEdgePointRay) < Vector2.Epsilon ? 0 : MathF.Sign(crossNextEdgePointRay);
            //Log.WriteLine(GetType() + ".IsTerminalExtEdgeValid: signs: " + sign1 + ", " + sign2 + " for edges: " + extEdge + " " + nextEdge);
            return sign2 == 0 || sign1 == sign2;
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

        protected void ForEachEdge(Triangle triangle, Action<int, EdgeEntry> action)
        {
            triangle.GetEdges(edgeBuffer);
            for (int i = 0; i < 3; i++)
            {
                action(GetEdgeKey(edgeBuffer[i]), edgeBuffer[i]);
            }
        }

        protected void RefreshExternalEdgesNextPrev(int startIndex = 0)
        {
            EdgeEntry.RefreshSortedEdgesNextPrev(extEdges, extEdgeCount, startIndex);
        }

        protected int GetExternalEdgeIndex(EdgeEntry edge, int startIndex = -1)
        {
            int edgeIndex = -1;
            int stepCounter = 0;
            if (startIndex >= 0 && startIndex < extEdgeCount)
            {
                if (extEdges[startIndex].Equals(edge))
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
                        nextResult = extEdges[next].Equals(edge);
                        result = nextResult || extEdges[prev].Equals(edge);
                        next = extEdges[next].Next; //GetNextIndex(next, extEdgeCount);
                        prev = extEdges[prev].Prev; //GetPrevIndex(prev, extEdgeCount);
                        stepCounter++;
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
                for (int i = 0; i <= halfEdgeCount; i++)
                {
                    if (extEdges[i].Equals(edge))
                    {
                        edgeIndex = i;
                        stepCounter = i;
                        break;
                    }
                    else if (extEdges[extEdgeCount - 1 - i].Equals(edge))
                    {
                        edgeIndex = extEdgeCount - 1 - i;
                        stepCounter = i;
                        break;
                    }
                }
            }
            Log.WriteLine(GetType() + ".GetExternalEdgeIndex: extEdgeCount: " + extEdgeCount + " edge: " + edge + " stepCounter: " + stepCounter + " edgeIndex: " + edgeIndex);
            if (edgeIndex < 0)
            {
                throw new Exception("GetExternalEdgeIndex: Edge Not Found: " + edge + " startIndex: " + startIndex);
            }
            return edgeIndex;
        }

        public bool IsEdgeExternal(EdgeEntry edge)
        {
            return IsEdgeExternal(edge, out _);
        }

        public bool IsEdgeInternal(int edgeKey)
        {
            return edgeCounterDict.TryGetValue(edgeKey, out int edgeCount) && edgeCount == 2;
        }

        private bool IsEdgeInternal(EdgeEntry edge, out int edgeKey)
        {
            return IsEdgeInternal(edgeKey = GetEdgeKey(edge));
        }

        private bool IsEdgeExternal(int edgeKey)
        {
            return edgeCounterDict.TryGetValue(edgeKey, out int edgeCount) && edgeCount == 1;
        }

        private bool IsEdgeExternal(EdgeEntry edge, out int edgeKey)
        {
            return IsEdgeExternal(edgeKey = GetEdgeKey(edge));
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

        protected int GetExternalEdgeKey(int edgeIndex)
        {
            return GetEdgeKey(extEdges[edgeIndex]);
        }

        public int GetEdgeKey(int edgeA, int edgeB)
        {
            if (edgeB > edgeA)
            {
                (edgeB, edgeA) = (edgeA, edgeB);
            }
            return edgeA * points.Length + edgeB;
        }

        public int GetEdgeKey(EdgeEntry edge)
        {
            return edge.A * points.Length + edge.B;
        }

        public EdgeEntry GetEdgeFromKey(int key)
        {
            int edgeA = key / points.Length;
            int edgeB = key % points.Length;
            return new EdgeEntry(edgeA, edgeB);
        }

        private void GetEdgeFromKey(int key, out int edgeA, out int edgeB)
        {
            edgeA = key / points.Length;
            edgeB = key % points.Length;
        }

        private Triangle GetExternalTriangle(EdgeEntry extEdge)
        {
            int edgeKey = GetEdgeKey(extEdge);
            if (extEdgeTriangleDict.TryGetValue(edgeKey, out long triangleKey))
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
