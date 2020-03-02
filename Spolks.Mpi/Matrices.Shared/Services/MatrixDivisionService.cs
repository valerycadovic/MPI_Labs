using System.Collections.Generic;

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


        public static IEnumerable<long> MultiplyFramе(
            int first, int last, int columns, Matrix2D<long> a, Matrix2D<long> b)
        {
            for (int absoluteIndex = first; absoluteIndex <= last; absoluteIndex++)
            {
                int i = absoluteIndex / columns;
                int j = absoluteIndex % columns;

                long sum = 0;
                for (int k = 0; k < columns; k++)
                    sum += a[i, k] * b[k, j];

                for (int v = 0; v < 5000; v++);

                yield return sum;
            }
        }
    }
}
