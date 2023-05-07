namespace Triangulation
{
    public readonly struct DeprecatedEdgeEntry
    {
        //public float SqrLength { get; private set; }

        //public void SetSqrLength(Vector2[] points)
        //{
        //    SqrLength = GetVector(points).LengthSquared();
        //}

        //public bool IsPointOnEdge(int pointIndex, Vector2[] points, out bool inRange)
        //{
        //    return IsPointOnEdge(points[pointIndex], points, out inRange, false);
        //}

        //public bool IsPointOnEdge(Vector2 point, Vector2[] points, out bool inRange)
        //{
        //    return IsPointOnEdge(point, points, out inRange, false);
        //}

        //public void SetLastPointDegenerateAngle(Vector2 point, Vector2[] points)
        //{
        //    LastPointDegenerateAngle = MakesDegenerateAngleWithPoint(point, points);
        //
        //    if (!LastPointDegenerateAngle && LastPointOpposite && LastPointInRange)
        //    {
        //        LastPointDegenerateTriangle = false;
        //    }
        //}

        //public bool MakesDegenerateAngleWithPoint(Vector2 point, Vector2[] points)
        //{
        //    var midPoint = GetMidPoint(points);
        //    var midRay = (midPoint - point).Normalized();
        //    bool degenerateDistA = points[A].GetSqrDistToLine(point, midRay) < DegenerateDistanceSqr;
        //    bool degenerateDistB = points[B].GetSqrDistToLine(point, midRay) < DegenerateDistanceSqr;
        //    return degenerateDistA || degenerateDistB;
        //}
    }
}
