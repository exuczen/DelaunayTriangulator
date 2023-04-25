namespace Triangulation
{
    public readonly struct DeprecatedTriangle
    {
        //private static readonly Vector2[] edgeVecBuffer = new Vector2[3];
        //private static readonly EdgeEntry[] edgeBuffer = new EdgeEntry[3];
        //private static readonly int[] indexBuffer = new int[3];

        //public static void SetKeys(Triangle[] triangles, int trianglesCount, int pointsLength, int[] indexBuffer)
        //{
        //    for (int i = 0; i < trianglesCount; i++)
        //    {
        //        triangles[i].SetKey(pointsLength, indexBuffer);
        //    }
        //}

        //public static bool IsDegenerate(int a, int b, int c, Vector2[] points)
        //{
        //    edgeVecBuffer[0] = points[b] - points[a];
        //    edgeVecBuffer[1] = points[c] - points[b];
        //    edgeVecBuffer[2] = points[a] - points[c];
        //    return Triangle.IsDegenerate(edgeVecBuffer, false);
        //}

        //public bool IsDegenerate(Vector2[] points)
        //{
        //    GetEdges(points, edgeVecBuffer);
        //    return IsDegenerate(edgeVecBuffer, false);
        //}

        //public bool IsDegenerate2(Vector2[] points, bool allEdges)
        //{
        //    GetEdges(edgeBuffer);
        //    GetIndices(indexBuffer, 1); // C, A, B
        //    if (allEdges)
        //    {
        //        for (int i = 0; i < 3; i++)
        //        {
        //            if (edgeBuffer[i].MakesDegenerateAngleWithPoint(points[indexBuffer[i]], points))
        //            {
        //                return true;
        //            }
        //        }
        //        return false;
        //    }
        //    else
        //    {
        //        float minSqrLength = edgeBuffer[0].GetVector(points).LengthSquared();
        //        int minIndex = 0;
        //        for (int i = 1; i < 3; i++)
        //        {
        //            float sqrLength = edgeBuffer[i].GetVector(points).LengthSquared();
        //            if (sqrLength < minSqrLength)
        //            {
        //                minSqrLength = sqrLength;
        //                minIndex = i;
        //            }
        //        }
        //        return edgeBuffer[minIndex].MakesDegenerateAngleWithPoint(points[indexBuffer[minIndex]], points);
        //    }
        //}
    }
}
