namespace Triangulation
{
    public enum ErrorCode
    {
        None,
        Undefined,
        Stop,
        BaseTriangulationFailed,
        InvalidTriangulation,
        InternalEdgeExists,
        ExternalEdgesBrokenLoop,
        PointOutsideTriangles,
    }

    public readonly struct ErrorData
    {
        public ErrorCode ErrorCode { get; }
        public int PointIndex { get; }

        public static ErrorData Create(ErrorCode errorCode, int pointIndex = -1)
        {
            return new ErrorData(errorCode, pointIndex);
        }

        public ErrorData(ErrorCode errorCode, int pointIndex = -1)
        {
            ErrorCode = errorCode;
            PointIndex = pointIndex;
        }
    }
}
