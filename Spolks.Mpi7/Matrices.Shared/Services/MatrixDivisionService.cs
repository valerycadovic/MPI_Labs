using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Matrices.Shared.Services
{
    public static class MatrixDivisionService
    {
        public static IEnumerable<BigInteger> MultiplyFrame(
            int first, int last, int columns, Matrix2D<BigInteger> a, Matrix2D<BigInteger> b)
        {
            for (int absoluteIndex = first; absoluteIndex <= last; absoluteIndex++)
            {
                int i = absoluteIndex / columns;
                int j = absoluteIndex % columns;

                yield return a.GetRow(i)
                    .Zip(b.GetColumn(j))
                    .Select(tuple => tuple.First * tuple.Second)
                    .Aggregate((f, s) => f + s);
            }
        }

        public static (int first, int last) GetFrameIndexes(this Matrix2D<BigInteger> matrix, int rank, int size)
        {
            return GetFrameIndexes(matrix.Size, rank, size);
        }

        public static (int first, int last) GetFrameIndexes(int matrixSize, int rank, int size)
        {
            int frameSize = matrixSize / size;
            int first = rank * frameSize;
            int last = rank == size - 1 ? matrixSize - 1 : first + frameSize - 1;

            return (first, last);
        }

        public static void CommitFrame(this Matrix2D<BigInteger> result, MatrixFrame frame)
        {
            for (int i = frame.First, j = 0; i <= frame.Last; i++, j++)
            {
                result[i] = frame.Results[j];
            }
        }

        public static void CommitFrame(this Matrix2D<BigInteger> result, int first, int last, BigInteger[] frames)
        {
            var frame = new MatrixFrame(first, last, frames);
            result.CommitFrame(frame);
        }
    }
}
