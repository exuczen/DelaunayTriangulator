using System.Numerics;

namespace Triangulation
{
    public interface IParticles
    {
        int Capacity { get; }
        void SetParticlePosition(int i, Vector2 position);
        void SetParticleActive(int i, bool active);
    }
}
