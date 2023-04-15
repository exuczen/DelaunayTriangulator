using System.Drawing;
using System;

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

        public static UnityEngine.Color32 ToColor32(this Color c)
        {
            return new UnityEngine.Color32(c.R, c.G, c.B, c.A);
        }
#endif

        public static Color ColorToColor(DeprecatedColor color)
        {
            return Color.FromArgb(color.argb);
        }

        public static Color ColorFromHSV(float hue, float saturation, float value)
        {
            int hi = Convert.ToInt32(MathF.Floor(hue / 60f)) % 6;
            float f = hue / 60f - MathF.Floor(hue / 60f);

            value *= 255f;
            int v = Convert.ToInt32(value);
            int p = Convert.ToInt32(value * (1f - saturation));
            int q = Convert.ToInt32(value * (1f - f * saturation));
            int t = Convert.ToInt32(value * (1f - (1f - f) * saturation));

            return hi switch
            {
                0 => Color.FromArgb(255, v, t, p),
                1 => Color.FromArgb(255, q, v, p),
                2 => Color.FromArgb(255, p, v, t),
                3 => Color.FromArgb(255, p, q, v),
                4 => Color.FromArgb(255, t, p, v),
                _ => Color.FromArgb(255, v, p, q),
            };
        }

        public static void ColorToHSV(Color color, out float hue, out float saturation, out float value)
        {
            int max = Math.Max(color.R, Math.Max(color.G, color.B));
            int min = Math.Min(color.R, Math.Min(color.G, color.B));

            hue = color.GetHue();
            saturation = (max == 0) ? 0 : 1f - (1f * min / max);
            value = max / 255f;
        }
    }
}
