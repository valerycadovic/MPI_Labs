using System;
using System.Numerics;

namespace Matrices.Shared
{
    [Serializable]
    public class MatrixFrame
    {
        public int First { get; }

        public int Last { get; }

        public BigInteger[] Results { get; }

        public MatrixFrame(int first, int last, BigInteger[] results)
        {
            First = first;
            Last = last;
            Results = results;
        }
    }
}
