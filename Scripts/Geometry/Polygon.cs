﻿//#define LOGS_ENABLED

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;

namespace Triangulation
{
    public class Polygon
    {
        private const float Lower180 = 179.95f;

        public float ReakRectTolerance { get; set; }
        public int PeakCount => edgePeaks.Count;
        public List<EdgePeak> EdgePeaks => edgePeaks;

        //public int SortedPeakCount => sortedPeaks.Count;
        //public bool IsConvex => SortedPeakCount > 0 && sortedPeaks[SortedPeakCount - 1].IsConvex;

        private readonly List<EdgePeak> edgePeaks = new List<EdgePeak>();
        private readonly List<EdgePeak> sortedPeaks = new List<EdgePeak>();
        private readonly List<Vector4Int> concaveRanges = new List<Vector4Int>();
        //private readonly int[] indexBuffer = new int[3];
        private readonly Vector2[] edgeBuffer = new Vector2[3];
        private readonly Polygon subPolygon = null;
        private readonly EdgePeakComparer edgePeakComparer = default;

        private int innerAngleSign = 0;

        public Polygon(bool createSubPolygon)
        {
            if (createSubPolygon)
            {
                subPolygon = new Polygon(false);
            }
        }

        public void Clear(bool clearSubPoly = true)
        {
            innerAngleSign = 0;
            edgePeaks.Clear();
            sortedPeaks.Clear();
            concaveRanges.Clear();
            if (clearSubPoly && subPolygon != null)
            {
                subPolygon.Clear();
            }
        }

        public void CopyTo(Polygon other)
        {
            other.Clear(other.subPolygon != this);
            other.innerAngleSign = innerAngleSign;
            other.edgePeaks.AddRange(edgePeaks);
            other.sortedPeaks.AddRange(sortedPeaks);
            other.concaveRanges.AddRange(concaveRanges);
        }

        public void SetFromExternalEdges(IncrementalEdgeInfo edgeInfo, Vector2[] points)
        {
            Clear();
            float angleSum = 0f;

            edgeInfo.ForEachExternalPeak((edge1, edge2) => {
                var peak = new EdgePeak(edge1, edge2, points);
                angleSum += peak.SetAngle360();
                edgePeaks.Add(peak);
            });
            if (PeakCount < 3)
            {
                throw new Exception("SetFromExternalEdges: PeakCount: " + PeakCount);
            }
            SetInnerAngleSign(angleSum);

            for (int i = 0; i < PeakCount; i++)
            {
                edgePeaks[i] = edgePeaks[i].SetupPeakRect(innerAngleSign, points);
#if LOGS_ENABLED
                Log.WriteLine(GetType() + ".SetFromExternalEdges: " + i + " " + edgePeaks[i]);
#endif
            }
            SortPeaksByAngle();
        }

        public void Triangulate(Triangle[] triangles, ref int trianglesCount, Vector2[] points)
        {
            if (PeakCount < 3)
            {
#if LOGS_ENABLED
                Log.WriteLine(GetType() + ".Triangulate: PeakCount: " + PeakCount);
#endif
                return;
            }
            else if (innerAngleSign == 0)
            {
                throw new Exception("Triangulate: innerAngleSign: " + innerAngleSign);
            }
            while (PeakCount > 3)
            {
                var peak = GetNextPeakToClip(points, out int peakIndex, out int sortedIndex);
                triangles[trianglesCount++] = peak.CreateTriangle(points);
#if LOGS_ENABLED
                //Log.WriteLine(GetType() + ".Triangulate: " + triangles[trianglesCount - 1]);
#endif
                ClipPeak(peakIndex, sortedIndex, points);
            }
            triangles[trianglesCount++] = edgePeaks[1].CreateTriangle(points);
#if LOGS_ENABLED
            //Log.WriteLine(GetType() + ".Triangulate: " + triangles[trianglesCount - 1]);
            //Log.PrintTriangles(triangles, trianglesCount, GetType() + ".Triangulate");
#endif
        }

        public int ClipConcavePeaks(Triangle[] triangles, Vector2[] points, IncrementalEdgeInfo edgeInfo)
        {
            int trianglesCount = 0;

            EdgePeak concavePeak;
            int sortedLast = sortedPeaks.Count - 1;

            while (sortedLast > 1 && (concavePeak = sortedPeaks[sortedLast]).Angle > 185f)
            {
                int peakIndex = GetPeakIndex(concavePeak.PeakVertex);

                ClipPeak(peakIndex, sortedLast, points);

                edgeInfo.ClipPeakExternalEdges(concavePeak, null);
                edgeInfo.SetPointExternal(concavePeak.PeakVertex, false);

                triangles[trianglesCount++] = concavePeak.CreateTriangle(points);
#if LOGS_ENABLED
                Log.WriteLine(GetType() + ".ClipConcavePeaks: " + triangles[trianglesCount - 1]);
#endif
                sortedLast = sortedPeaks.Count - 1;
            }
            return trianglesCount;
        }

        public void ClipPeak(int peakIndex, int sortedIndex, Vector2[] points)
        {
            if (PeakCount < 3)
            {
#if LOGS_ENABLED
                Log.WriteLine(GetType() + ".ClipPeak: PeakCount: " + PeakCount);
#endif
                return;
            }
            var edgePeak = edgePeaks[peakIndex];

            GetPrevNextPeaksAfterClip(peakIndex, out var prevPeak, out var nextPeak, points);
            RemovePeakFromSorted(edgePeak, sortedIndex);
            edgePeaks.RemoveAt(peakIndex);

            int peakPrev = IndexRange.GetPrev(peakIndex, PeakCount);
            prevPeak = ReplacePeak(peakPrev, prevPeak, points);

            int peakNext = peakIndex % PeakCount;
            nextPeak = ReplacePeak(peakNext, nextPeak, points);
#if LOGS_ENABLED
            Log.WriteLine(GetType() + ".ClipPeak: " + edgePeak + " prevPeak: " + prevPeak + " nextPeak: " + nextPeak);
#endif
        }

        public bool CanClipPeak(int peakIndex, Vector2[] points, float pointCellSize)
        {
            if (PeakCount <= 3)
            {
                return false;
            }
            var peak = edgePeaks[peakIndex];
            if (peak.Angle > Lower180)
            {
                return true;
            }
            else if (PeakContainsConcave(peak, points))
            {
                return false;
            }
            else if (IsLastSortedPeakConvex())
            {
                return true;
            }
            else
            {
                float minDistToOppEdge = GetOtherPeakMinDistToOppEdge(peakIndex, points);
                bool result = minDistToOppEdge > pointCellSize * 0.5f;
#if LOGS_ENABLED
                Log.WriteLine(GetType() + ".CanClipPeak: " + result + " | minDistToOppEdge / pointCellSize = " + (minDistToOppEdge / pointCellSize).ToString("f2"));
#endif
                return result;
            }
        }

        public EdgePeak GetPeak(int peakVertex, out int peakIndex)
        {
            peakIndex = GetPeakIndex(peakVertex);
            if (peakIndex >= 0)
            {
                return edgePeaks[peakIndex];
            }
            else
            {
                throw new Exception("GetConvexPeak: " + peakVertex);
            }
        }

        public int TriangulateFromConcavePeaks(EdgePeak peak, IndexRange extEdgesRange, IncrementalEdgeInfo baseEdgeInfo, Triangle[] triangles, IncrementalEdgeInfo addedEdgeInfo)
        {
            int trianglesCount = 0;

            InvalidateExternalEdgesRange(extEdgesRange, out var validRange);

            if (SortConcavePeaksByDistToOppEdge(peak, baseEdgeInfo.Points))
            {
                if (TriangulateFromConcavePeaks(validRange, baseEdgeInfo, triangles, out trianglesCount))
                {
                    AddExternalEdgesFromConcaveRanges(validRange, addedEdgeInfo);
                }
            }
            return trianglesCount;
        }

        private bool TriangulateFromConcavePeaks(IndexRange validRange, IncrementalEdgeInfo baseEdgeInfo, Triangle[] triangles, out int trianglesCount)
        {
            trianglesCount = 0;

            EdgePeak concavePeak;
            var points = baseEdgeInfo.Points;
            int sortedLast = sortedPeaks.Count - 1;
            bool result = false;

            while (sortedLast > 1 && (concavePeak = sortedPeaks[sortedLast]).Angle >= 180f)
            {
#if LOGS_ENABLED
                Log.WriteLine(GetType() + ".TriangulateFromConcavePeak: ---------------- " + concavePeak + " validRange: " + validRange);
#endif
                int concaveVertex = concavePeak.PeakVertex;
                int concavePeakIndex = GetPeakIndex(concaveVertex);
                int prevTrianglesCount = trianglesCount;
                TriangulateFromConcavePeak(concavePeakIndex, false, baseEdgeInfo, triangles, ref trianglesCount, out int peakBeg);
                TriangulateFromConcavePeak(concavePeakIndex, true, baseEdgeInfo, triangles, ref trianglesCount, out int peakEnd);
                var begPeak = edgePeaks[peakBeg];
                var endPeak = edgePeaks[peakEnd];
                bool concavePeakTriangleValid = IsConcavePeakTriangleValid(concavePeak, peakBeg, peakEnd, points);
                if (concavePeakTriangleValid)
                {
                    int begVertex = begPeak.PeakVertex;
                    int endVertex = endPeak.PeakVertex;
                    triangles[trianglesCount++] = new Triangle(begVertex, concaveVertex, endVertex, points);
                }
                // Add and invalidate concave range
                {
                    if (trianglesCount > prevTrianglesCount)
                    {
                        var range = new Vector3Int(
                            begPeak.IsValid ? peakBeg : -1,
                            concavePeakTriangleValid ? -1 : concavePeakIndex,
                            endPeak.IsValid ? peakEnd : -1
                        );
                        int offset = IndexRange.GetIndexCount(validRange.Beg, peakBeg, PeakCount);
                        concaveRanges.Add(new Vector4Int(range, offset));
                    }
                    InvalidatePeaksRange(GetNextPeakIndex(peakBeg), GetPrevPeakIndex(peakEnd));
                }
                sortedLast = sortedPeaks.Count - 1;
                result = true;
            }
            return result;
        }

        private void AddExternalEdgesInConcaveRange(Vector4Int range, EdgeEntry[] addedExtEdges, ref int edgeCount)
        {
            int peakBeg = range.v[0];
            int peakCcv = range.v[1];
            int peakEnd = range.v[2];
            int begVertex = peakBeg >= 0 ? edgePeaks[peakBeg].PeakVertex : -1;
            int endVertex = peakEnd >= 0 ? edgePeaks[peakEnd].PeakVertex : -1;
            if (peakCcv < 0)
            {
                if (begVertex < 0 || endVertex < 0)
                {
                    throw new Exception("AddExternalEdgesInConcaveRange: begVertex: " + begVertex + " endVertex: " + endVertex);
                }
                addedExtEdges[edgeCount++] = new EdgeEntry(begVertex, endVertex);
#if LOGS_ENABLED
                Log.WriteLine(GetType() + ".AddExternalEdgesInConcaveRange: " + addedExtEdges[edgeCount - 1]);
#endif
            }
            else
            {
                if (begVertex < 0 && endVertex < 0)
                {
                    throw new Exception("AddExternalEdgesInConcaveRange: begVertex: " + begVertex + " endVertex: " + endVertex);
                }
                int ccvVertex = edgePeaks[peakCcv].PeakVertex;
                if (begVertex >= 0)
                {
                    addedExtEdges[edgeCount++] = new EdgeEntry(begVertex, ccvVertex);
#if LOGS_ENABLED
                    Log.WriteLine(GetType() + ".AddExternalEdgesInConcaveRange: " + addedExtEdges[edgeCount - 1]);
#endif
                }
                if (endVertex >= 0)
                {
                    addedExtEdges[edgeCount++] = new EdgeEntry(ccvVertex, endVertex);
#if LOGS_ENABLED
                    Log.WriteLine(GetType() + ".AddExternalEdgesInConcaveRange: " + addedExtEdges[edgeCount - 1]);
#endif
                }
            }
        }

        private void AddExternalEdgesFromConcaveRanges(IndexRange validRange, IncrementalEdgeInfo addedEdgeInfo)
        {
            int rangesCount = concaveRanges.Count;
            int edgeCount = 0;
            var addedExtEdges = addedEdgeInfo.AddedExtEdges;

            void addEdgesInRange(int beg, int end)
            {
                ForEachPeakInRange(beg, end, (peak, peakIndex) => {
#if LOGS_ENABLED
                    Log.WriteLine(GetType() + ".AddExternalEdgesFromConcaveRanges: " + peak.EdgeB + " (peak.EdgeB)");
#endif
                    addedExtEdges[edgeCount++] = peak.EdgeB;
                });
            }
            void addEdgesInConcaveRange(Vector4Int concaveRange) => AddExternalEdgesInConcaveRange(concaveRange, addedExtEdges, ref edgeCount);

            if (rangesCount == 0)
            {
                addEdgesInRange(validRange.Beg, GetPrevPeakIndex(validRange.End));
            }
            else
            {
                concaveRanges.Sort((a, b) => a.w.CompareTo(b.w));
#if LOGS_ENABLED
                PrintConcaveRanges(concaveRanges, "AddExternalEdgesFromConcaveRanges: " + rangesCount);
#endif
                var peakRange = concaveRanges[0];
                int peakBeg = GetConcaveRangeTerminal(peakRange, 0);

                if (validRange.Beg != peakBeg)
                {
                    addEdgesInRange(validRange.Beg, GetPrevPeakIndex(peakBeg));
                }
                if (rangesCount > 1)
                {
#if LOGS_ENABLED
                    Log.WriteLine(GetType() + ".AddExternalEdgesFromConcaveRanges: rangesCount: " + rangesCount);
#endif
                    for (int i = 0; i < rangesCount - 1; i++)
                    {
                        peakRange = concaveRanges[i];
                        addEdgesInConcaveRange(peakRange);

                        var peakEnd = GetConcaveRangeTerminal(peakRange, 2);
                        var nextRange = concaveRanges[i + 1];
                        var nextBeg = GetConcaveRangeTerminal(nextRange, 0);
                        if (peakEnd != nextBeg)
                        {
                            addEdgesInRange(peakEnd, GetPrevPeakIndex(nextBeg));
                        }
                    }
                }
                // Add last range
                {
                    peakRange = concaveRanges[rangesCount - 1];
                    int peakEnd = GetConcaveRangeTerminal(peakRange, 2);

                    addEdgesInConcaveRange(peakRange);

                    if (peakEnd != validRange.End)
                    {
                        addEdgesInRange(peakEnd, GetPrevPeakIndex(validRange.End));
                    }
                }
                concaveRanges.Clear();
            }
            EdgeEntry.RefreshSortedEdgesNextPrev(addedExtEdges, edgeCount);
            EdgeEntry.SetLastElementCount(addedExtEdges, edgeCount);
#if LOGS_ENABLED
            Log.PrintArray(addedExtEdges, edgeCount, "AddExternalEdgesFromConcaveRanges: rangesCount: " + rangesCount);
#endif
        }

        private int GetConcaveRangeTerminal(Vector4Int range, int i)
        {
            if (i != 0 && i != 2)
            {
                throw new Exception("GetConcaveRangeTerminal: " + i + " range: " + ConcaveRangeToString(range));
            }
            int terminal = range[i] >= 0 ? range[i] : range[1];
            if (terminal < 0)
            {
                throw new Exception("GetConcaveRangeTerminal: " + i + " range: " + ConcaveRangeToString(range));
            }
            return terminal;
        }

        private static IndexRange GetPeaksRange(IndexRange extEdgesRange)
        {
            int beg = extEdgesRange.Beg;
            int end = extEdgesRange.GetPrev(extEdgesRange.End);
            return new IndexRange(beg, end, extEdgesRange.FullLength);
        }

        private void InvalidateExternalEdgesRange(IndexRange extEdgesRange, out IndexRange validPeaksRange)
        {
            var peaksRange = GetPeaksRange(extEdgesRange);
            InvalidatePeaksRange(peaksRange);
            validPeaksRange = peaksRange.GetInverseRange();
        }

        private void InvalidatePeaksRange(int beg, int end)
        {
            InvalidatePeaksRange(new IndexRange(beg, end, PeakCount));
        }

        private void InvalidatePeak(int peakIndex)
        {
            var peak = edgePeaks[peakIndex];
            RemovePeakFromSorted(peak, -1);
            edgePeaks[peakIndex] = peak.Invalidate();
        }

        private void InvalidatePeaksRange(IndexRange range)
        {
            ForEachPeakInRange(range, (peak, peakIndex) => {
#if LOGS_ENABLED
                Log.WriteLine(GetType() + ".InvalidatePeaksRange: " + peak);
#endif
                InvalidatePeak(peakIndex);
            });
#if LOGS_ENABLED
            //Log.PrintEdgePeaks(edgePeaks, "InvalidatePeaksRange: peaks: ");
            //Log.PrintEdgePeaks(sortedPeaks, "InvalidatePeaksRange: sortedPeaks: ");
#endif
        }

        private bool IsPeakRangeValid(int peakBeg, int peakEnd)
        {
            bool valid = true;
            ForEachPeakInRange(peakBeg, peakEnd, (peak, peakIndex) => {
                valid &= peak.IsValid;
            });
            return valid;
        }

        private bool IsConcavePeakTriangleValid(EdgePeak concavePeak, int peakBeg, int peakEnd, Vector2[] points)
        {
            if (peakEnd < 0 || peakBeg < 0 || !IsPeakRangeValid(peakBeg, peakEnd))
            {
                return false;
            }
            bool result = false;
            int concaveVertex = concavePeak.PeakVertex;
            int begVertex = edgePeaks[peakBeg].PeakVertex;
            int endVertex = edgePeaks[peakEnd].PeakVertex;
            var concavePoint = points[concaveVertex];
            edgeBuffer[1] = points[endVertex] - concavePoint; // b-c -> c-b
            edgeBuffer[2] = points[begVertex] - concavePoint; // a-c
            float crossRays = innerAngleSign * Mathv.Cross(edgeBuffer[1], edgeBuffer[2]);
            if (crossRays > 0f)
            {
                edgeBuffer[0] = points[endVertex] - points[begVertex]; // b-a
                edgeBuffer[1] = -edgeBuffer[1];
                result = !Triangle.IsDegenerate(edgeBuffer, false);
            }
#if LOGS_ENABLED
            Log.WriteLine(GetType() + ".IsConcavePeakTriangleValid: crossRays: " + crossRays.ToString("f2") + " begVertex: " + begVertex + " endVertex: " + endVertex + " concaveVertex: " + concaveVertex + " valid: " + result);
#endif
            return result;
        }

        private void TriangulateFromConcavePeak(int concavePeakIndex, bool forward, IncrementalEdgeInfo baseEdgeInfo, Triangle[] triangles, ref int trianglesCount, out int endPeakIndex)
        {
            int peakEnd = endPeakIndex = GetMaxAnglePeakFromPeak(concavePeakIndex, forward, baseEdgeInfo);
            int peakBeg;
            if (endPeakIndex < 0)
            {
                endPeakIndex = GetNextPeakIndexGetter(forward)(concavePeakIndex);
                return;
            }
#if LOGS_ENABLED
            Log.WriteLine(GetType() + ".TriangulateFromConcavePeak: endPeakVertex: " + edgePeaks[endPeakIndex].PeakVertex);
#endif
            if (forward)
            {
                peakBeg = concavePeakIndex;
            }
            else
            {
                peakBeg = peakEnd;
                peakEnd = concavePeakIndex;
            }
            var range = new IndexRange(peakBeg, peakEnd, PeakCount);
            TriangulatePeakRange(range, triangles, ref trianglesCount, baseEdgeInfo.Points);
        }

        private void TriangulatePeakRange(IndexRange range, Triangle[] triangles, ref int trianglesCount, Vector2[] points)
        {
            if (range.Beg < 0 || range.End < 0)
            {
                return;
            }
            int rangeCount = range.GetIndexCount();
            if (rangeCount < 3)
            {
                return;
            }
            else if (rangeCount == 3)
            {
                var midPeak = GetNextPeak(range.Beg, out _);
                if (!midPeak.MakesDegenerateTriangle(points, edgeBuffer))
                {
                    triangles[trianglesCount++] = midPeak.CreateTriangle(points);
#if LOGS_ENABLED
                    Log.WriteLine(GetType() + ".TriangulatePeakRange: " + triangles[trianglesCount - 1]);
#endif
                }
                else
                {
                    InvalidatePeaksRange(range);
#if LOGS_ENABLED
                    Log.WriteLine(GetType() + ".TriangulatePeakRange: SKIPPED DEGENERATE TRIANGLE midPeak: " + midPeak);
#endif
                }
            }
            else
            {
                subPolygon.SetFromPolygonRange(this, range, points);
                subPolygon.Triangulate(triangles, ref trianglesCount, points);
            }
        }

        private void SetFromPolygonRange(Polygon source, IndexRange range, Vector2[] points)
        {
            Clear();

            int rangeCount = range.GetIndexCount();
            if (rangeCount < 3)
            {
                return;
            }
            ReakRectTolerance = source.ReakRectTolerance;
            innerAngleSign = source.innerAngleSign;

            var sourcePeaks = source.edgePeaks;

            var begPeak = sourcePeaks[range.Beg];
            var endPeak = sourcePeaks[range.End];
            var loopEdge = new EdgeEntry(endPeak.PeakVertex, begPeak.PeakVertex);

            begPeak = new EdgePeak(loopEdge, begPeak.EdgeB, points);
            endPeak = new EdgePeak(endPeak.EdgeA, loopEdge, points);
            begPeak.Setup(innerAngleSign, points);
            endPeak.Setup(innerAngleSign, points);

            var midRange = new IndexRange(range.GetNext(range.Beg), range.GetPrev(range.End), range.FullLength);

            edgePeaks.Add(begPeak);
            source.ForEachPeakInRange(midRange, (peak, peakIndex) => {
                edgePeaks.Add(peak);
            });
            edgePeaks.Add(endPeak);

            SortPeaksByAngle();
#if LOGS_ENABLED
            Log.PrintList(edgePeaks, "SetFromPolygonRange: peaks: ");
            Log.PrintList(sortedPeaks, "SetFromPolygonRange: sortedPeaks: ");
#endif
        }

        private int GetMaxAnglePeakFromPeak(int begPeakIndex, bool forward, IncrementalEdgeInfo baseEdgeInfo)
        {
            var points = baseEdgeInfo.Points;
            var begPeak = edgePeaks[begPeakIndex];
            var begPoint = points[begPeak.PeakVertex];
            int begSign;
            EdgeEntry begEdge;
            Vector2 begEdgeVec;
            Func<EdgePeak, Vector2> getPrevEdgeVec;
            Func<EdgePeak, EdgeEntry> getPrevEdge;
            var getNextPeakIndex = GetNextPeakIndexGetter(forward);
            if (forward)
            {
                begSign = -innerAngleSign;
                begEdge = begPeak.EdgeB;
                begEdgeVec = begPeak.EdgeVecB.Normalized();
                getPrevEdge = peak => peak.EdgeA;
                getPrevEdgeVec = peak => peak.EdgeVecA;
            }
            else
            {
                begSign = innerAngleSign;
                begEdge = begPeak.EdgeA;
                begEdgeVec = begPeak.EdgeVecA.Normalized();
                getPrevEdge = peak => peak.EdgeB;
                getPrevEdgeVec = peak => peak.EdgeVecB;
            }
            //var prevPeakPoint = points[edgePeaks[getNextPeakIndex(begPeakIndex)].PeakVertex];
            var prevPeakRay = begEdgeVec;

            float minCosAngle = Maths.Cos1Deg;
            int maxPeakIndex = -1;
            var maxPeakRay = begEdgeVec;

            void action(EdgePeak peak, int peakIndex)
            {
                var peakPoint = points[peak.PeakVertex];
                var peakRay = (peakPoint - begPoint).Normalized();
                float sinAngle = begSign * Mathv.Cross(peakRay, begEdgeVec);
                float cosAngle = Vector2.Dot(peakRay, begEdgeVec);
                if (sinAngle > 0f && cosAngle < minCosAngle)
                {
                    minCosAngle = cosAngle;

                    edgeBuffer[0] = peakRay;
                    edgeBuffer[1] = getPrevEdgeVec(peak).Normalized();
                    edgeBuffer[2] = -prevPeakRay;

                    cosAngle = Vector2.Dot(peakRay, maxPeakRay);

                    var prevEdge = getPrevEdge(peak);
                    bool prevEdgeInternal = baseEdgeInfo.IsEdgeInternal(prevEdge);
                    bool triangleDegenerate = !prevEdgeInternal && Triangle.IsDegenerate(edgeBuffer, true);

                    if (cosAngle < Maths.Cos1Deg && !triangleDegenerate)
                    {
                        maxPeakIndex = peakIndex;
                        maxPeakRay = peakRay;
                    }
                }
#if LOGS_ENABLED
                //Log.WriteLine(GetType() + ".GetMaxAnglePeakFromPeak: PREV EDGE VALIDATION: " + getPrevEdgeVec(peak).Equals(prevPeakPoint - peakPoint), Vector2.Epsilon);
                //prevPeakPoint = peakPoint;
                Log.WriteLine(GetType() + ".GetMaxAnglePeakFromPeak: " + begEdge + " ray:(" + begPeak.PeakVertex + " -> " + peak.PeakVertex + ") " + sinAngle.ToString("f4") + " prevEdge: " + getPrevEdge(peak));
#endif
                prevPeakRay = peakRay;
            }
            ForEachValidPeakFromPeak(getNextPeakIndex(begPeakIndex), forward, action);

            return maxPeakIndex;
        }

        private void ForEachPeakInRange(int beg, int end, Action<EdgePeak, int> action)
        {
            ForEachPeakInRange(new IndexRange(beg, end, PeakCount), action);
        }

        private void ForEachPeakInRange(IndexRange range, Action<EdgePeak, int> action)
        {
            int peakIndex = range.Beg;
            while (peakIndex != range.End)
            {
                action(edgePeaks[peakIndex], peakIndex);
                peakIndex = range.GetNext(peakIndex);
            }
            action(edgePeaks[range.End], peakIndex);
        }

        private void ForEachValidPeakFromPeak(int peakIndex, bool forward, Action<EdgePeak, int> action)
        {
            int count = 0;
            var getNextPeakIndex = GetNextPeakIndexGetter(forward);
            peakIndex = getNextPeakIndex(peakIndex);
            var peak = edgePeaks[peakIndex];
            while (peak.IsValid && count < PeakCount - 1)
            {
                action(peak, peakIndex);
                peakIndex = getNextPeakIndex(peakIndex);
                peak = edgePeaks[peakIndex];
                count++;
            }
        }

        private void SortPeaksByAngle()
        {
            sortedPeaks.Clear();
            sortedPeaks.AddRange(edgePeaks);
            sortedPeaks.Sort((a, b) => a.Angle.CompareTo(b.Angle));
#if LOGS_ENABLED
            //Log.PrintEdgePeaks(sortedPeaks, "SortPeaksByAngle: ");
#endif
        }

        private void SetInnerAngleSign(float angleSum)
        {
            if (MathF.Abs(angleSum - (PeakCount - 2) * 180f) > 1f)
            {
                angleSum = 0f;
                for (int i = 0; i < PeakCount; i++)
                {
                    var peak = edgePeaks[i];
                    angleSum += peak.InvertAngle();
                    edgePeaks[i] = peak;
                }
                if (MathF.Abs(angleSum - (PeakCount - 2) * 180f) > 1f)
                {
                    throw new Exception("SetupPeakAngles: angleSum: " + angleSum + " PeakCount: " + PeakCount);
                }
                innerAngleSign = -1;
            }
            else
            {
                innerAngleSign = 1;
            }
#if LOGS_ENABLED
            //Log.WriteLine(GetType() + ".SetInnerAngleSign: angleSum: " + angleSum);
#endif
        }

        private void GetPrevNextPeaksAfterClip(int clipPeakIndex, out EdgePeak prevPeak, out EdgePeak nextPeak, Vector2[] points)
        {
            int peakIndex = clipPeakIndex;
            var peak = edgePeaks[peakIndex];
            var oppEdge = peak.GetOppositeEdge(out _);

            prevPeak = GetPrevPeak(peakIndex, out _);
            nextPeak = GetNextPeak(peakIndex, out _);
            var prevEdge = prevPeak.EdgeA;
            var nextEdge = nextPeak.EdgeB;
            prevPeak = new EdgePeak(prevEdge, oppEdge, points);
            nextPeak = new EdgePeak(oppEdge, nextEdge, points);
        }

        private Func<EdgePeak, EdgeEntry> GetNextEdgeGetter(bool forward, bool next)
        {
            if (forward == next)
            {
                return peak => peak.EdgeB;
            }
            else
            {
                return peak => peak.EdgeA;
            }
        }

        private Func<int, int> GetNextPeakIndexGetter(bool forward)
        {
            if (forward)
            {
                return GetNextPeakIndex;
            }
            else
            {
                return GetPrevPeakIndex;
            }
        }

        private bool GetSortedPeakIndex(int peakVertex, out int sortedIndex)
        {
            sortedIndex = sortedPeaks.FindIndex(p => p.PeakVertex == peakVertex);
            return sortedIndex >= 0;
        }

        private int GetPeakIndex(int peakVertex)
        {
            return edgePeaks.FindIndex(p => p.PeakVertex == peakVertex);
        }

        private int GetPrevPeakIndex(int peakIndex)
        {
            return IndexRange.GetPrev(peakIndex, PeakCount);
        }

        private int GetNextPeakIndex(int peakIndex)
        {
            return IndexRange.GetNext(peakIndex, PeakCount);
        }

        private EdgePeak GetPrevPeak(int peakIndex, out int peakPrev)
        {
            peakPrev = GetPrevPeakIndex(peakIndex);
            return edgePeaks[peakPrev];
        }

        private EdgePeak GetNextPeak(int peakIndex, out int peakNext)
        {
            peakNext = GetNextPeakIndex(peakIndex);
            return edgePeaks[peakNext];
        }

        private EdgePeak ReplacePeak(int peakIndex, EdgePeak newPeak, Vector2[] points)
        {
            var peak = edgePeaks[peakIndex];
            RemovePeakFromSorted(peak, -1);
            peak = newPeak;
            peak = peak.Setup(innerAngleSign, points);
            edgePeaks[peakIndex] = peak;
            AddPeakToSorted(peak);
            return peak;
        }

        private EdgePeak ReplacePeak(int peakIndex, EdgeEntry edge1, EdgeEntry edge2, Vector2[] points)
        {
            var newPeak = new EdgePeak(edge1, edge2, points);
            return ReplacePeak(peakIndex, newPeak, points);
        }

        private void AddPeakToSorted(EdgePeak peak)
        {
            int peakIndex = 0;
            while (peakIndex < sortedPeaks.Count && peak.Angle > sortedPeaks[peakIndex].Angle)
            {
                peakIndex++;
            }
            if (peakIndex < sortedPeaks.Count)
            {
                sortedPeaks.Insert(peakIndex, peak);
            }
            else
            {
                sortedPeaks.Add(peak);
            }
        }

        private void RemovePeakFromSorted(EdgePeak peak, int sortedIndex)
        {
            if (sortedIndex >= 0)
            {
                sortedPeaks.RemoveAt(sortedIndex);
            }
            else if (GetSortedPeakIndex(peak.PeakVertex, out sortedIndex))
            {
                sortedPeaks.RemoveAt(sortedIndex);
            }
            //else
            //{
            //    Log.WriteWarning("RemovePeakFromSorted: " + peak.PeakVertex + " not found in sortedPeaks");
            //}
        }

        private EdgePeak GetNextPeakToClip(Vector2[] points, out int peakIndex, out int sortedIndex)
        {
            peakIndex = -1;
            sortedIndex = -1;
            if (sortedPeaks.Count == 0)
            {
                throw new Exception("GetNextPeakToClip: sortedPeaks.Count == 0");
            }
            int peaksCount = sortedPeaks.Count;
            EdgePeak convexPeak;
            int convexIndex = 0;

            while (convexIndex < peaksCount && (convexPeak = sortedPeaks[convexIndex]).Angle <= Lower180)
            {
#if LOGS_ENABLED
                //Log.WriteLine(GetType() + ".GetMinAnglePeakIndex: convexPeak: " + convexPeak + " convexIndex: " + convexIndex);
#endif
                if (!PeakContainsConcave(convexPeak, points))
                {
                    sortedIndex = convexIndex;
                    peakIndex = GetPeakIndex(convexPeak.PeakVertex);
                    return convexPeak;
                }
                convexIndex++;
            }
            throw new Exception("GetNextPeakToClip: convex peak not containing concave peak not found");
        }

        private bool PeakContainsConcave(EdgePeak convexPeak, Vector2[] points)
        {
            //if (convexPeak.Angle > 180f)
            //{
            //    return false;
            //}
            int peakVertex = convexPeak.PeakVertex;
            int prevPeakVertex = convexPeak.VertexA;
            int nextPeakVertex = convexPeak.VertexB;

            int concaveIndex = sortedPeaks.Count - 1;
            EdgePeak concavePeak;

            while (concaveIndex > 0 && (concavePeak = sortedPeaks[concaveIndex--]).Angle > Lower180)
            {
#if LOGS_ENABLED
                //Log.WriteLine(GetType() + ".PeakContainsConcave: concavePeak: " + concavePeak);
#endif
                int concaveVertex = concavePeak.PeakVertex;
                var concavePoint = points[concaveVertex];
                bool concaveSeparate = concaveVertex != prevPeakVertex && concaveVertex != nextPeakVertex && concaveVertex != peakVertex;
                if (concaveSeparate && convexPeak.PeakRect.ContainsPoint(concavePoint, ReakRectTolerance))
                {
#if LOGS_ENABLED
                    //Log.WriteLine(GetType() + ".PeakContainsConcave: " + convexPeak + " PeakRect contains " + concaveVertex + " of " + concavePeak);
#endif
                    return true;
                }
            }
            return false;
        }

        private bool IsLastSortedPeakConvex()
        {
            int sortedLast = sortedPeaks.Count - 1;
            if (sortedLast < 0)
            {
                return false;
            }
            return sortedPeaks[sortedLast].IsConvex;
        }

        private bool SortConcavePeaksByDistToOppEdge(EdgePeak oppPeak, Vector2[] points)
        {
            int sortedCount = sortedPeaks.Count;
            int sortedLast = sortedCount - 1;
            if (sortedCount < 2)
            {
                return false;
            }
            var concavePeak = sortedPeaks[sortedLast];
            if (concavePeak.IsConvex)
            {
#if LOGS_ENABLED
                Log.WriteLine(GetType() + ".SortConcavePeaksByDistToOppEdge: Last peak is convex: " + concavePeak);
#endif
                return false;
            }
            var oppEdge = oppPeak.GetOppositeEdge(out _);
            var oppEdgeN2 = oppPeak.PeakRect.N2;
            var oppPeakPoint = points[oppPeak.VertexA];
            int oppAngleSign = oppPeak.AngleSign;

            Debug.Assert(oppPeak.IsConvex);
            Debug.Assert(oppAngleSign == 0 || oppAngleSign == innerAngleSign);

            while (sortedLast >= 0 && (concavePeak = sortedPeaks[sortedLast]).Angle > 180f)
            {
                Debug.Assert(oppPeak.PeakVertex != concavePeak.PeakVertex);

                var peakPoint = points[concavePeak.PeakVertex];
                var ray = peakPoint - oppPeakPoint;
                float dist = Vector2.Dot(ray, oppEdgeN2);

                sortedPeaks[sortedLast--] = concavePeak.SetValueToCompare(dist);
#if LOGS_ENABLED
                Log.WriteLine(GetType() + ".SortConcavePeaksByDistToOppEdge: " + concavePeak.PeakVertex + " dist to " + oppEdge + " : " + dist);
#endif
            }
            sortedPeaks.Sort(++sortedLast, sortedCount - sortedLast, edgePeakComparer);
#if LOGS_ENABLED
            Log.WriteLine(GetType() + ".SortConcavePeaksByDistToOppEdge: sortedLast: " + sortedLast + " count: " + (sortedCount - sortedLast) + " | oppPeakRect: " + oppPeak.PeakRect);
            Log.PrintList(sortedPeaks, peak => peak.ToLongString(), "SortConcavePeaksByDistToOppEdge: ");
#endif
            return true;
        }

        private float GetOtherPeakMinDistToOppEdge(int peakIndex, Vector2[] points)
        {
            var peak = edgePeaks[peakIndex];
            float oppEdgeLength = peak.PeakRect.Size.X;
            var oppEdgeN1 = -peak.PeakRect.N1;
#if LOGS_ENABLED
            var oppEdge = peak.GetOppositeEdge(out _);
#endif
            int beg = GetNextPeakIndex(peakIndex);
            int end = GetPrevPeakIndex(peakIndex);
            int next = GetNextPeakIndex(beg);

            var begPeakVertex = edgePeaks[beg].PeakVertex;
            var begPeakPoint = points[begPeakVertex];

            float minSqrDist = float.MaxValue;

            while (next != end)
            {
                var peakVertex = edgePeaks[next].PeakVertex;
                var peakPoint = points[peakVertex];

                var ray = peakPoint - begPeakPoint;
                float rayDotEdge = Vector2.Dot(ray, oppEdgeN1);
                bool inRange = rayDotEdge >= 0f && rayDotEdge <= oppEdgeLength;
#if LOGS_ENABLED
                Log.WriteLine(GetType() + ".GetMinSqrDistPeakToOppEdge: " + oppEdge.ToShortString() + " dot (" + begPeakVertex + ", " + peakVertex + ") = " + rayDotEdge.ToString("f2") + " oppEdgeLength: " + oppEdgeLength.ToString("f2"));
#endif
                if (inRange)
                {
                    var rayN2 = ray - rayDotEdge * oppEdgeN1;
                    minSqrDist = MathF.Min(rayN2.LengthSquared(), minSqrDist);
                }
                next = GetNextPeakIndex(next);
            }
            return MathF.Sqrt(minSqrDist);
        }

        //private static float GetSqrDistToEdge(Vector2 point, Vector2 edgePoint, Vector2 edgeRayN, float edgeLength, bool inRange)
        //{
        //    var ray = point - edgePoint;
        //    float rayDotEdge = Vector2.Dot(ray, edgeRayN);
        //    if (!inRange || rayDotEdge >= 0f && rayDotEdge <= edgeLength)
        //    {
        //        var rayN2 = ray - rayDotEdge * edgeRayN;
        //        return rayN2.SqrLength;
        //    }
        //    return -1f;
        //}

        private string ConcaveRangeToString(Vector4Int range)
        {
            var sb = Log.StringBuilder;
            for (int i = 0; i < 3; i++)
            {
                int peakIndex = range[i];
                sb.AppendFormat("{0} | ", peakIndex >= 0 ? edgePeaks[peakIndex].PeakVertex : peakIndex);
            }
            sb.AppendFormat("({0})", range.w);
            string s = sb.ToString();
            sb.Clear();
            return s;
        }

#if LOGS_ENABLED
        private void PrintConcaveRange(Vector4Int range)
        {
            Log.WriteLine(GetType() + ".PrintConcaveRange: " + ConcaveRangeToString(range));
        }

        private void PrintConcaveRanges(List<Vector4Int> ranges, string prefix)
        {
            Log.WriteLine(GetType() + ".PrintConcaveRanges: " + prefix);
            for (int i = 0; i < ranges.Count; i++)
            {
                PrintConcaveRange(ranges[i]);
            }
        }
#endif
    }
}
