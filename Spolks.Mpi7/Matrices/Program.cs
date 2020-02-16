using System;
using System.Linq;
using System.Numerics;
using Matrices.Services;
using MPI;

namespace Matrices
{
    class Program
    {
        static void Main(string[] args)
        {
            var matrixA = MatrixService.InitializeByNaturalNumbers(5, 6);
            var matrixB = MatrixService.InitializeByNaturalNumbers(6, 5);

            using var environment = MpiEnvironmentProvider.MpiEnvironment;

            var matrixC = matrixA.ClusteredMultiplyBy(matrixB);
            Matrix2D<BigInteger> matrixD = null;

            if (matrixC != null)
            {
                matrixD = matrixA.MultiplyBy(matrixB);
                PrintMatrix(matrixA, true);
                PrintMatrix(matrixB, true);
                PrintMatrix(matrixD, true);
                PrintMatrix(matrixC, true);
                Console.WriteLine(CompareMatrices(matrixD, matrixC));
            }

            Communicator.world.Barrier();

            var matrixE = matrixA.ClusteredMultiplyByAsync(matrixB);

            if (matrixE != null)
            {
                matrixD = matrixA.MultiplyBy(matrixB);
                PrintMatrix(matrixE, true);
                Console.WriteLine(CompareMatrices(matrixD, matrixE));
            }
        }

        static bool CompareMatrices(Matrix2D<BigInteger> a, Matrix2D<BigInteger> b)
        {
            if (a.Columns != b.Columns || a.Rows != b.Rows)
            {
                return false;
            }

            return a.Zip(b).All(tuple => tuple.First == tuple.Second);
        }

        static void PrintMatrix(Matrix2D<BigInteger> matrix, bool withDelimiter = false)
        {
            foreach (var row in matrix.GetRows())
            {
                foreach (var item in row)
                {
                    Console.Write($"{item}\t");
                }
                Console.WriteLine();
            }

            if (withDelimiter)
            {
                Console.WriteLine("------------------------------------");
            }
        }
    }
}
