#if UNITY_EDITOR || UNITY_STANDALONE
#define UNITY
#endif

namespace Triangulation
{
    public static class ColorUtils
    {
#if UNITY
        public static UnityEngine.Color32 Color32FromARGB(int argb)
        {
            byte b = (byte)(argb >>= 0 & 0xff);
            byte g = (byte)(argb >>= 8 & 0xff);
            byte r = (byte)(argb >>= 8 & 0xff);
            byte a = (byte)(argb >>= 8 & 0xff);
            return new UnityEngine.Color32(r, g, b, a);
        }

        public static UnityEngine.Color32 ToColor32(this System.Drawing.Color c)
        {
            return new UnityEngine.Color32(c.R, c.G, c.B, c.A);
        }
#endif
    }
}
