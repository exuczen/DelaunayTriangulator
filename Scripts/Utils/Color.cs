namespace Triangulation
{
    public struct Color
    {
        public static readonly Color Azure = new(0xFFF0FFFF);
        public static readonly Color Blue = new(0xFF0000FF);
        public static readonly Color Black = new(0xFF000000);
        public static readonly Color FloralWhite = new(0xFFFFFAF0);
        public static readonly Color LightGreen = new(0xFF90EE90);
        public static readonly Color White = new(0xFFFFFFFF);
        public static readonly Color Red = new(0xFFFF0000);

        public int argb;

        public Color(uint hex)
        {
            argb = (int)hex;
        }
    }
}
