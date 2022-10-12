namespace Triangulation
{
    public struct IndexRange
    {
        public static readonly IndexRange None = new IndexRange(-1);

        public int FullLength { get; private set; }

        public int Beg;
        public int End;

        public static int GetNext(int index, int length) => (index + 1) % length;

        public static int GetPrev(int index, int length) => (index - 1 + length) % length;

        public static int GetIndexCount(int beg, int end, int length)
        {
            if (length < 0)
            {
                return -1;
            }
            else if (end >= beg)
            {
                return end - beg + 1;
            }
            else
            {
                return length - (beg - end) + 1;
            }
        }

        public IndexRange(int fullLength)
        {
            FullLength = fullLength;
            Beg = -1;
            End = -1;
        }

        public IndexRange(int beg, int end, int fullLength)
        {
            Beg = beg;
            End = end;
            FullLength = fullLength;
        }

        public int GetIndexCount() => GetIndexCount(Beg, End, FullLength);

        public int GetNext(int index) => GetNext(index, FullLength);

        public int GetPrev(int index) => GetPrev(index, FullLength);

        public IndexRange GetInverseRange()
        {
            return new IndexRange(GetNext(End), GetPrev(Beg), FullLength);
        }

        public override string ToString()
        {
            return string.Format("IndexRange: {0} - {1} | {2}", Beg, End, FullLength);
        }
    }
}
