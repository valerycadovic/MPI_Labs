using System;
using System.Linq;
using System.Numerics;
using MPI;
using MpiEnvironment = MPI.Environment;

namespace Matrices.Services
{
    public static class MatrixService
    {
        public static Matrix2D<BigInteger> MultiplyBy(this Matrix2D<BigInteger> self, Matrix2D<BigInteger> multiplier)
        {
            if (self.Columns != multiplier.Rows)
            {
                throw new ArithmeticException("these matrices cannot be multiplied due to sizes mismatch");
            }

            var result = Matrix2D<BigInteger>.CreateEmpty(self.Rows, multiplier.Columns);

            for (int i = 0; i < self.Rows; i++)
            {
                for (int j = 0; j < multiplier.Columns; j++)
                {
                    result[i, j] = self.GetRow(i)
                        .Zip(multiplier.GetColumn(j))
                        .Select(tuple => tuple.First * tuple.Second)
                        .Aggregate((a, b) => a + b);
                }
            }

            return result;
        }

        public static Matrix2D<BigInteger> GenerateRandomly(int rows, int columns, int min = 0, int max = 100)
        {
            var result = Matrix2D<BigInteger>.CreateEmpty(rows, columns);
            var random = new Random();

            for (int i = 0; i < result.Rows; i++)
            {
                for (int j = 0; j < result.Columns; j++)
                {
                    result[i, j] = random.Next(min, max);
                }
            }

            return result;
        }

        public static Matrix2D<BigInteger> InitializeByNaturalNumbers(int rows, int columns)
        {
            var result = Matrix2D<BigInteger>.CreateEmpty(rows, columns);

            for (int i = 0; i < result.Size; i++)
            {
                result[i] = i;
            }

            return result;
        }
    }
}
