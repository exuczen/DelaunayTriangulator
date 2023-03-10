using System;

namespace Triangulation
{
    public interface IExceptionThrower
    {
        void ThrowException(Exception exception);
        void ThrowException(string message, ErrorCode errorCode, int pointIndex = -1);
    }
}
