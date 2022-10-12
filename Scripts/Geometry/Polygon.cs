using System;
using System.Collections.Generic;

namespace Triangulation
{
    public class Polygon
    {
        public float Tolerance { get; set; }
        public int PeakCount => edgePeaks.Count;
        public List<EdgePeak> EdgePeaks => edgePeaks;

        private readonly List<EdgePeak> edgePeaks = new List<EdgePeak>();
        private readonly List<EdgePeak> sortedPeaks = new List<EdgePeak>();
        private readonly List<Vector4Int> concaveRanges = new List<Vector4Int>();
        //private readonly int[] indexBuffer = new int[3];
        private readonly Vector2[] edgeBuffer = new Vector2[3];
        private readonly Polygon subPolygon = null;

        private int innerAngleSign = 0;

        public Polygon(bool createSubPolygon)
        {
            if (createSubPolygon)
            {
                subPolygon = new Polygon(false);
            }
        }

        public void Clear()
        {
            innerAngleSign = 0;
            edgePeaks.Clear();
            sortedPeaks.Clear();
            concaveRanges.Clear();
            if (subPolygon != null)
            {
                subPolygon.Clear();
            }
        }

        public void SetFromExternalEdges(EdgeInfo edgeInfo, Vector2[] points)
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
                edgePeaks[i] = edgePeaks[i].Setup(points);
                Console.WriteLine(GetType() + ".SetFromExternalEdges: " + i + " " + edgePeaks[i]);
            }
            SortPeaksByAngle();
        }

        public void Triangulate(Triangle[] triangles, ref int trianglesCount, Vector2[] points)
        {
            if (PeakCount < 3)
            {
                Console.WriteLine(GetType() + ".Triangulate: PeakCount: " + PeakCount);
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
                //Console.WriteLine(GetType() + ".Triangulate: " + triangles[trianglesCount - 1]);

                ClipPeak(peakIndex, sortedIndex, points);
            }
            triangles[trianglesCount++] = edgePeaks[1].CreateTriangle(points);
            //Console.WriteLine(GetType() + ".Triangulate: " + triangles[trianglesCount - 1]);

            //Log.PrintTriangles(triangles, trianglesCount, GetType() + ".Triangulate");
        }

        public void ClipPeak(int peakIndex, int sortedIndex, Vector2[] points)
        {
            if (PeakCount < 3)
            {
                Console.WriteLine(GetType() + ".ClipPeak: PeakCount: " + PeakCount);
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

            Console.WriteLine(GetType() + ".ClipPeak: " + edgePeak + " prevPeak: " + prevPeak + " nextPeak: " + nextPeak);
        }

        public bool CanClipPeak(int peakIndex, Vector2[] points, out bool containsConcave)
        {
            if (PeakCount <= 3)
            {
                containsConcave = false;
                return false;
            }
            var peak = edgePeaks[peakIndex];
            containsConcave = peak.IsConvex && PeakContainsConcave(peak, points);
            if (!containsConcave)
            {
                GetPrevNextPeaksAfterClip(peakIndex, out var prevPeak, out var nextPeak, points);
                bool prevPeakTriangleDegenerate = prevPeak.MakesDegenerateTriangle(points, edgeBuffer);
                bool nextPeakTriangleDegenerate = nextPeak.MakesDegenerateTriangle(points, edgeBuffer);
                Console.WriteLine(GetType() + ".CanClipPeak: " + !prevPeakTriangleDegenerate + " " + !nextPeakTriangleDegenerate + " | prevPeak: " + prevPeak + " nextPeak: " + nextPeak);
                return !prevPeakTriangleDegenerate && !nextPeakTriangleDegenerate;
            }
            else
            {
                return false;
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

        public bool PeakContainsConcave(EdgePeak convexPeak, Vector2[] points)
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

            while (concaveIndex > 0 && (concavePeak = sortedPeaks[concaveIndex--]).Angle > 179f)
            {
                //Console.WriteLine(GetType() + ".PeakContainsConcave: concavePeak: " + concavePeak);
                int concaveVertex = concavePeak.PeakVertex;
                var concavePoint = points[concaveVertex];
                bool concaveSeparate = concaveVertex != prevPeakVertex && concaveVertex != nextPeakVertex && concaveVertex != peakVertex;
                if (concaveSeparate && convexPeak.PeakRect.ContainsPoint(concavePoint, Tolerance))
                {
                    //Console.WriteLine(GetType() + ".PeakContainsConcave: " + convexPeak + " PeakRect contains " + concaveVertex + " of " + concavePeak);
                    return true;
                }
            }
            return false;
        }

        public int TriangulateFromConcavePeaks(IndexRange extEdgesRange, Triangle[] triangles, Vector2[] points, EdgeInfo addedEdgeInfo)
        {
            InvalidateExternalEdgesRange(extEdgesRange, out var validRange);

            int sortedPeaksCount = sortedPeaks.Count;
            if (sortedPeaksCount < 2)
            {
                return 0;
            }
            var concavePeak = sortedPeaks[sortedPeaksCount - 1];
            if (concavePeak.Angle < 180f)
            {
                Console.WriteLine(GetType() + ".TriangulateFromConcavePeak: Last peak is convex: " + concavePeak);
                return 0;
            }
            int trianglesCount = 0;

            while (sortedPeaksCount > 2 && concavePeak.Angle >= 180f)
            {
                Console.WriteLine(GetType() + ".TriangulateFromConcavePeak: ---------------- " + concavePeak + " validRange: " + validRange);
                int concaveVertex = concavePeak.PeakVertex;
                int concavePeakIndex = GetPeakIndex(concaveVertex);
                int prevTrianglesCount = trianglesCount;
                TriangulateFromConcavePeak(concavePeakIndex, false, triangles, ref trianglesCount, points, out int peakBeg);
                TriangulateFromConcavePeak(concavePeakIndex, true, triangles, ref trianglesCount, points, out int peakEnd);
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
                sortedPeaksCount = sortedPeaks.Count;
                concavePeak = sortedPeaks[sortedPeaksCount - 1];
            }
            AddExternalEdgesFromConcaveRanges(validRange, addedEdgeInfo);

            return trianglesCount;
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
                Console.WriteLine(GetType() + ".AddExternalEdgesInConcaveRange: " + addedExtEdges[edgeCount - 1]);
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
                    Console.WriteLine(GetType() + ".AddExternalEdgesInConcaveRange: " + addedExtEdges[edgeCount - 1]);
                }
                if (endVertex >= 0)
                {
                    addedExtEdges[edgeCount++] = new EdgeEntry(ccvVertex, endVertex);
                    Console.WriteLine(GetType() + ".AddExternalEdgesInConcaveRange: " + addedExtEdges[edgeCount - 1]);
                }
            }
        }

        private void AddExternalEdgesFromConcaveRanges(IndexRange validRange, EdgeInfo addedEdgeInfo)
        {
            int rangesCount = concaveRanges.Count;
            int edgeCount = 0;
            var addedExtEdges = addedEdgeInfo.AddedExtEdges;

            void addEdgesInRange(int beg, int end)
            {
                ForEachPeakInRange(beg, end, (peak, peakIndex) => {
                    Console.WriteLine(GetType() + ".AddExternalEdgesFromConcaveRanges: " + peak.EdgeB + " (peak.EdgeB)");
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

                PrintConcaveRanges(concaveRanges, "AddExternalEdgesFromConcaveRanges: " + rangesCount);

                var peakRange = concaveRanges[0];
                int peakBeg = GetConcaveRangeTerminal(peakRange, 0);

                if (validRange.Beg != peakBeg)
                {
                    addEdgesInRange(validRange.Beg, GetPrevPeakIndex(peakBeg));
                }
                if (rangesCount > 1)
                {
                    Console.WriteLine(GetType() + ".AddExternalEdgesFromConcaveRanges: rangesCount: " + rangesCount);

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
            Log.PrintEdges(addedExtEdges, edgeCount, "AddExternalEdgesFromConcaveRanges: rangesCount: " + rangesCount);
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

        private IndexRange GetPeaksRange(IndexRange extEdgesRange)
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

        private void InvalidatePeaksRange(IndexRange range)
        {
            ForEachPeakInRange(range, (peak, peakIndex) => {
                Console.WriteLine(GetType() + ".InvalidatePeaksRange: " + peak);
                RemovePeakFromSorted(peak, -1);
                edgePeaks[peakIndex] = peak.Invalidate();
            });
            //Log.PrintEdgePeaks(edgePeaks, "InvalidatePeaksRange: peaks: ");
            //Log.PrintEdgePeaks(sortedPeaks, "InvalidatePeaksRange: sortedPeaks: ");
        }

        private bool IsConcavePeakTriangleValid(EdgePeak concavePeak, int peakBeg, int peakEnd, Vector2[] points)
        {
            if (peakEnd < 0 || peakBeg < 0)
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
            float crossRays = innerAngleSign * Vector2.Cross(edgeBuffer[1], edgeBuffer[2]);
            if (crossRays > 0f)
            {
                edgeBuffer[0] = points[endVertex] - points[begVertex]; // b-a
                edgeBuffer[1] = -edgeBuffer[1];
                result = !Triangle.IsDegenerate(edgeBuffer, false);
            }
            Console.WriteLine(GetType() + ".IsConcavePeakTriangleValid: crossRays: " + crossRays.ToString("f2") + " begVertex: " + begVertex + " endVertex: " + endVertex + " concaveVertex: " + concaveVertex + " valid: " + result);
            return result;
        }

        private void TriangulateFromConcavePeak(int concavePeakIndex, bool forward, Triangle[] triangles, ref int trianglesCount, Vector2[] points, out int endPeakIndex)
        {
            int peakEnd = endPeakIndex = GetMaxAnglePeakFromPeak(concavePeakIndex, forward, points);
            int peakBeg;
            if (endPeakIndex < 0)
            {
                endPeakIndex = GetNextPeakIndexGetter(forward)(concavePeakIndex);
                return;
            }
            Console.WriteLine(GetType() + ".TriangulateFromConcavePeak: endPeakVertex: " + edgePeaks[endPeakIndex].PeakVertex);
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
            TriangulatePeakRange(range, triangles, ref trianglesCount, points);
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
                    Console.WriteLine(GetType() + ".TriangulatePeakRange: " + midPeak.CreateTriangle(points));
                    triangles[trianglesCount++] = midPeak.CreateTriangle(points);
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
            Tolerance = source.Tolerance;
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

            Log.PrintEdgePeaks(edgePeaks, "SetFromPolygonRange: peaks: ");
            Log.PrintEdgePeaks(sortedPeaks, "SetFromPolygonRange: sortedPeaks: ");
        }

        private int GetMaxAnglePeakFromPeak(int begPeakIndex, bool forward, Vector2[] points)
        {
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
                float sinAngle = begSign * Vector2.Cross(peakRay, begEdgeVec);
                float cosAngle = Vector2.Dot(peakRay, begEdgeVec);
                if (sinAngle > 0f && cosAngle < minCosAngle)
                {
                    minCosAngle = cosAngle;

                    edgeBuffer[0] = peakRay;
                    edgeBuffer[1] = getPrevEdgeVec(peak).Normalized();
                    edgeBuffer[2] = -prevPeakRay;

                    cosAngle = Vector2.Dot(peakRay, maxPeakRay);

                    if (cosAngle < Maths.Cos1Deg && !Triangle.IsDegenerate(edgeBuffer, true))
                    {
                        maxPeakIndex = peakIndex;
                        maxPeakRay = peakRay;
                    }
                }
                //Console.WriteLine(GetType() + ".GetMaxAnglePeakFromPeak: PREV EDGE VALIDATION: " + getPrevEdgeVec(peak).Equals(prevPeakPoint - peakPoint), Vector2.Epsilon);
                //prevPeakPoint = peakPoint;
                prevPeakRay = peakRay;
                Console.WriteLine(GetType() + ".GetMaxAnglePeakFromPeak: " + begEdge + " ray:(" + begPeak.PeakVertex + " -> " + peak.PeakVertex + ") " + sinAngle.ToString("f4") + " prevEdge: " + getPrevEdge(peak));
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
            //Log.PrintEdgePeaks(sortedPeaks, "SortPeaksByAngle: ");
        }

        private void SetInnerAngleSign(float angleSum)
        {
            if (Math.Abs(angleSum - (PeakCount - 2) * 180f) > 1f)
            {
                angleSum = 0f;
                for (int i = 0; i < PeakCount; i++)
                {
                    var peak = edgePeaks[i];
                    angleSum += peak.InvertAngle();
                    edgePeaks[i] = peak;
                }
                if (Math.Abs(angleSum - (PeakCount - 2) * 180f) > 1f)
                {
                    throw new Exception("SetupPeakAngles: angleSum: " + angleSum);
                }
                innerAngleSign = -1;
            }
            else
            {
                innerAngleSign = 1;
            }
            //Console.WriteLine(GetType() + ".SetInnerAngleSign: angleSum: " + angleSum);
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
            else
            {
                sortedPeaks.RemoveAt(sortedPeaks.FindIndex(p => p.PeakVertex == peak.PeakVertex));
            }
        }

        private EdgePeak GetNextPeakToClip(Vector2[] points, out int peakIndex, out int sortedIndex)
        {
            peakIndex = -1;
            sortedIndex = -1;
            if (sortedPeaks.Count == 0)
            {
                return default;
            }
            int peaksCount = sortedPeaks.Count;
            EdgePeak convexPeak;
            int convexIndex = 0;

            while (convexIndex < peaksCount && (convexPeak = sortedPeaks[convexIndex]).Angle <= 179f)
            {
                //Console.WriteLine(GetType() + ".GetMinAnglePeakIndex: convexPeak: " + convexPeak + " convexIndex: " + convexIndex);
                if (!PeakContainsConcave(convexPeak, points))
                {
                    sortedIndex = convexIndex;
                    peakIndex = GetPeakIndex(convexPeak.PeakVertex);
                    return convexPeak;
                }
                convexIndex++;
            }
            return default;
        }

        private string ConcaveRangeToString(Vector4Int range)
        {
            string s = "";
            for (int i = 0; i < 3; i++)
            {
                int peakIndex = range[i];
                s += (peakIndex >= 0 ? edgePeaks[peakIndex].PeakVertex.ToString() : peakIndex.ToString()) + " | ";
            }
            s += "(" + range.w + ")";
            return s;
        }

        private void PrintConcaveRange(Vector4Int range)
        {
            Console.WriteLine(GetType() + ".PrintConcaveRange: " + ConcaveRangeToString(range));
        }

        private void PrintConcaveRanges(List<Vector4Int> ranges, string prefix)
        {
            Console.WriteLine(GetType() + ".PrintConcaveRanges: " + prefix);
            for (int i = 0; i < ranges.Count; i++)
            {
                PrintConcaveRange(ranges[i]);
            }
        }
    }
}
