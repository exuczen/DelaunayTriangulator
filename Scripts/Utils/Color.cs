namespace Triangulation
{
    public struct Color
    {
        public static readonly Color Azure = new Color(0xFFF0FFFF);
        public static readonly Color Blue = new Color(0xFF0000FF);
        public static readonly Color Black = new Color(0xFF000000);
        public static readonly Color FloralWhite = new Color(0xFFFFFAF0);
        public static readonly Color LightGreen = new Color(0xFF90EE90);
        public static readonly Color White = new Color(0xFFFFFFFF);
        public static readonly Color Red = new Color(0xFFFF0000);

        public int argb;

        public Color(uint hex)
        {
            argb = (int)hex;
        }
    }
}
