namespace Triangulation
{
    public struct SerializedVector2
    {
        public float X { get; set; }
        public float Y { get; set; }

        public SerializedVector2(Vector2 v)
        {
            X = v.x;
            Y = v.y;
        }
    }
}
