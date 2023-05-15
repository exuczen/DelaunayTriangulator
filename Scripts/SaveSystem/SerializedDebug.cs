using System;
using System.Collections.Generic;

namespace Triangulation
{
    [Serializable]
    public class SerializedDebug
    {
#if UNITY
        public List<int> Indices = new();
        public List<SerializedVector2> Points = new();
        public int Index;
#else
        public List<int> Indices { get; set; } = new();
        public List<SerializedVector2> Points { get; set; } = new();
        public int Index { get; set; }
#endif

        public void Clear()
        {
            Indices.Clear();
            Points.Clear();
            Index = 0;
        }
    }
}
