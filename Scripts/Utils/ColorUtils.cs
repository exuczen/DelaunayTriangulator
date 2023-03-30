#if UNITY_EDITOR || UNITY_STANDALONE
#define UNITY
#endif

namespace Triangulation
{
    public static class ColorUtils
    {
#if UNITY
        public static UnityEngine.Color32 ToColor32(this System.Drawing.Color c)
        {
            return new UnityEngine.Color32(c.R, c.G, c.B, c.A);
        }
#endif
    }
}
