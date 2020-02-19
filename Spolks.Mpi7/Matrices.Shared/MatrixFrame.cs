using System;

namespace Matrices.Shared
{
    [Serializable]
    public class MatrixFrame
    {
        public int First { get; }

        public int Last { get; }

        public long[] Results { get; }

        public MatrixFrame(int first, int last, long[] results)
        {
            First = first;
            Last = last;
            Results = results;
        }
    }
}
