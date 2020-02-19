using System;
using System.Numerics;
using Matrices.Mpi7.Services;
using Matrices.Shared;
using Matrices.Shared.Services;
using MPI;
using MpiEnvironment = MPI.Environment;
using static MPI.Unsafe;
using static Matrices.Shared.Services.MatrixService;

namespace Matrices.Mpi7
{
    class Program
    {
        static void Main(string[] args)
        {
            int matrixARows = int.Parse(args[0]);
            int matrixAColumns = int.Parse(args[1]);
            int matrixBRows = matrixAColumns;
            int matrixBColumns = int.Parse(args[2]);

            var matrixA = InitializeByNaturalNumbers(matrixARows, matrixAColumns);
            var matrixB = InitializeByNaturalNumbers(matrixBRows, matrixBColumns);

            string[] mpiArgs = { };
            using var environment = new MpiEnvironment(ref mpiArgs);

            double startBlocking = MPI_Wtime();
            var matrixC = matrixA.ClusteredMultiplyBy(matrixB);
            double endBlocking = MPI_Wtime();
            Matrix2D<BigInteger> matrixD = null;

            if (matrixC != null)
            {
                double startNonParallel = MPI_Wtime();
                matrixD = matrixA.MultiplyBy(matrixB);
                double endNonParallel = MPI_Wtime();

                Console.WriteLine($"Non parallel execution time: {endNonParallel - startNonParallel}");
                Console.WriteLine($"Parallel blocking time: {endBlocking - startBlocking}");
                Console.WriteLine($"Non parallel and blocking results equality: {CompareMatrices(matrixD, matrixC)}");
            }

            Communicator.world.Barrier();

            double startNonBlocking = MPI_Wtime();
            var matrixE = matrixA.ClusteredMultiplyByAsync(matrixB);
            double endNonBlocking = MPI_Wtime();

            if (matrixE != null)
            {
                Console.WriteLine($"Non parallel and non blocking results equality: {CompareMatrices(matrixD, matrixE)}");
                Console.WriteLine($"Parallel non blocking time: {endNonBlocking - startNonBlocking}");
            }
        }
    }
}
