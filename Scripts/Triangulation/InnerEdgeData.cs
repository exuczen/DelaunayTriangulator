using System;

namespace Triangulation
{
    public struct InnerEdgeData
    {
        public long Triangle1Key { private set; get; }
        public long Triangle2Key { private set; get; }
        public bool Checked;

        public InnerEdgeData(long triangle1Key = -1, long triangle2Key = -1)
        {
            Triangle1Key = triangle1Key;
            Triangle2Key = triangle2Key;
            Checked = false;
        }

        public bool IsTriangleClear => Triangle1Key < 0 || Triangle2Key < 0;

        public bool IsClear => Triangle1Key < 0 && Triangle2Key < 0;

        public long GetValidTriangleKey()
        {
            if (IsClear)
            {
                throw new Exception("GetValidTriangleKey: IsClear");
            }
            else
            {
                return Triangle1Key >= 0 ? Triangle1Key : Triangle2Key;
            }
        }

        public void ClearTriangleKey(long triangleKey)
        {
            if (Triangle1Key == triangleKey)
            {
                Triangle1Key = -1;
            }
            else if (Triangle2Key == triangleKey)
            {
                Triangle2Key = -1;
            }
        }

        public void SetTriangleKey(long triangleKey)
        {
            if (Triangle1Key < 0)
            {
                Triangle1Key = triangleKey;
            }
            else if (Triangle2Key < 0)
            {
                Triangle2Key = triangleKey;
            }
            else
            {
                throw new Exception("SetTriangleKey: " + Triangle1Key + " " + Triangle2Key + " " + triangleKey);
            }
        }

        public override string ToString()
        {
            return string.Format("InnerEdgeData: {0}, {1}", Triangle1Key, Triangle2Key);
        }
    }
}
