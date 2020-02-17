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
            int matrixARows = int.Parse(args[0]);
            int matrixAColumns = int.Parse(args[1]);
            int matrixBRows = matrixAColumns;
            int matrixBColumns = int.Parse(args[2]);

            var matrixA = MatrixService.InitializeByNaturalNumbers(matrixARows, matrixAColumns);
            var matrixB = MatrixService.InitializeByNaturalNumbers(matrixBRows, matrixBColumns);

            using var environment = MpiEnvironmentProvider.MpiEnvironment;

            double startBlocking = Unsafe.MPI_Wtime();
            var matrixC = matrixA.ClusteredMultiplyBy(matrixB);
            double endBlocking = Unsafe.MPI_Wtime();
            Matrix2D<BigInteger> matrixD = null;

            if (matrixC != null)
            {
                double startNonParallel = Unsafe.MPI_Wtime();
                matrixD = matrixA.MultiplyBy(matrixB);
                double endNonParallel = Unsafe.MPI_Wtime();

                Console.WriteLine($"Non parallel execution time: {endNonParallel - startNonParallel}");
                Console.WriteLine($"Parallel blocking time: {endBlocking - startBlocking}");
                Console.WriteLine($"Non parallel and blocking results equality: {CompareMatrices(matrixD, matrixC)}");
            }

            Communicator.world.Barrier();

            double startNonBlocking = Unsafe.MPI_Wtime();
            var matrixE = matrixA.ClusteredMultiplyByAsync(matrixB);
            double endNonBlocking = Unsafe.MPI_Wtime();

            if (matrixE != null)
            {
                Console.WriteLine($"Non parallel and non blocking results equality: {CompareMatrices(matrixD, matrixE)}");
                Console.WriteLine($"Parallel non blocking time: {endNonBlocking - startNonBlocking}");
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
