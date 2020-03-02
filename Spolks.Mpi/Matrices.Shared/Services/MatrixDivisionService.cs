using System.Collections.Generic;
using System.Linq;

namespace Matrices.Shared.Services
{
    public static class MatrixDivisionService
    {
        public static IEnumerable<long> MultiplyFrame(
            int first, int last, int columns, Matrix2D<long> a, Matrix2D<long> b)
        {
            for (int absoluteIndex = first; absoluteIndex <= last; absoluteIndex++)
            {
                int i = absoluteIndex / columns;
                int j = absoluteIndex % columns;

                long sum = 0;
                for (int k = 0; k < columns; k++)
                    sum += a[i, k] * b[k, j];

                yield return sum;
            }
        }

        public static (int first, int last) GetFrameIndexes(this Matrix2D<long> matrix, int rank, int size)
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

        public static void CommitFrame(this Matrix2D<long> result, MatrixFrame frame)
        {
            for (int i = frame.First, j = 0; i <= frame.Last; i++, j++)
            {
                result[i] = frame.Results[j];
            }
        }

        public static void CommitFrame(this Matrix2D<long> result, int first, int last, long[] frames)
        {
            var frame = new MatrixFrame(first, last, frames);
            result.CommitFrame(frame);
        }
    }
}
