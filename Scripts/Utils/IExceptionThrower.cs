using System;

namespace Triangulation
{
    public interface IExceptionThrower
    {
        void ThrowException(Exception exception);
        void ThrowException(string message);
    }
}
