namespace Triangulation
{
    public struct DeprecatedColor
    {
        public static readonly DeprecatedColor Azure = new(0xFFF0FFFF);
        public static readonly DeprecatedColor Blue = new(0xFF0000FF);
        public static readonly DeprecatedColor Black = new(0xFF000000);
        public static readonly DeprecatedColor FloralWhite = new(0xFFFFFAF0);
        public static readonly DeprecatedColor Green = new(0xFF00FF00);
        public static readonly DeprecatedColor LightGreen = new(0xFF90EE90);
        public static readonly DeprecatedColor Red = new(0xFFFF0000);
        public static readonly DeprecatedColor White = new(0xFFFFFFFF);
        public static readonly DeprecatedColor Yellow = new(0xFFFFFF00);

        public int argb;

        public DeprecatedColor(uint hex)
        {
            argb = (int)hex;
        }
    }
}
