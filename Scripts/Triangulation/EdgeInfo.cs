using System;
using System.Collections.Generic;
using System.Numerics;

namespace Triangulation
{
    public class EdgeInfo
    {
        public Vector2[] Points => points;
        public Dictionary<int, int> EdgeCounterDict => edgeCounterDict;

        protected readonly Dictionary<int, int> edgeCounterDict = new Dictionary<int, int>();

        protected readonly Vector2[] points = null;
        protected readonly bool[] pointsExternal = null;

        protected readonly EdgeEntry[] edgeBuffer = new EdgeEntry[3];
        protected readonly int[] edgeKeyBuffer = new int[3];

        protected readonly IExceptionThrower exceptionThrower = null;

        public EdgeInfo(Vector2[] points, IExceptionThrower exceptionThrower)
        {
            this.points = points;
            this.exceptionThrower = exceptionThrower;

            pointsExternal = new bool[points.Length];
        }

        #region Logs
        public void PrintEdgeCounterDict(string prefix = null)
        {
            PrintEdgeDict(edgeCounterDict, ".PrintEdgeCounterDict: " + edgeCounterDict.Count + " " + prefix);
        }

        protected void PrintEdgeDict<T>(Dictionary<int, T> dict, string prefix)
        {
            Log.WriteLine(GetType() + prefix);
            foreach (var kvp in dict)
            {
                GetEdgeFromKey(kvp.Key, out int edgeA, out int edgeB);
                Log.WriteLine(GetType() + ".PrintEdgeDict: edge: " + edgeA + " " + edgeB + " value: " + kvp.Value);
            }
        }
        #endregion

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

        public void AddEdgesToCounterDict(Triangle[] triangles, int trianglesCount)
        {
            //PrintEdgeCounterDict("I");
            for (int i = trianglesCount - 1; i >= 0; i--)
            {
                AddEdgesToCounterDict(triangles[i]);
            }
            //PrintEdgeCounterDict("II");
        }

        public void RemoveEdgesFromCounterDict(Triangle triangle)
        {
            ForEachEdgeInCounterDict(triangle, (edgeKey, edge, edgeCount) => {
                if (--edgeCounterDict[edgeKey] <= 0)
                {
                    edgeCounterDict.Remove(edgeKey);
                }
            });
            //Log.WriteLine("RemoveEdgesFromCounterDict: " + triangle);
            //PrintEdgeCounterDict("RemoveEdgesFromCounterDict: ");
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

        public void GetEdgeFromKey(int key, out int edgeA, out int edgeB)
        {
            edgeA = key / points.Length;
            edgeB = key % points.Length;
        }

        public bool IsEdgeExternal(EdgeEntry edge)
        {
            return IsEdgeExternal(edge, out _);
        }

        public bool IsEdgeInternal(EdgeEntry edge)
        {
            return IsEdgeInternal(edge, out _);
        }

        public bool IsEdgeInternal(int edgeKey)
        {
            return edgeCounterDict.TryGetValue(edgeKey, out int edgeCount) && edgeCount == 2;
        }

        protected bool IsEdgeInternal(EdgeEntry edge, out int edgeKey)
        {
            return IsEdgeInternal(edgeKey = GetEdgeKey(edge));
        }

        protected bool IsEdgeExternal(int edgeKey)
        {
            return edgeCounterDict.TryGetValue(edgeKey, out int edgeCount) && edgeCount == 1;
        }

        protected bool IsEdgeExternal(EdgeEntry edge, out int edgeKey)
        {
            return IsEdgeExternal(edgeKey = GetEdgeKey(edge));
        }

        protected void ForEachEdgeInCounterDict(Triangle triangle, Action<int, EdgeEntry, int> action)
        {
            ForEachEdge(triangle, (edgeKey, edge) => {
                if (edgeCounterDict.TryGetValue(edgeKey, out int edgeCount))
                {
                    action(edgeKey, edge, edgeCount);
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
    }
}
