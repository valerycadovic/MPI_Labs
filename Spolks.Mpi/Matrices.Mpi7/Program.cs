using System;
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

            string[] mpiArgs = { };
            using var environment = new MpiEnvironment(ref mpiArgs);

            Matrix2D<long> matrixA;
            Matrix2D<long> matrixB;
            Matrix2D<long> controlMatrix = null;

            if (Communicator.world.Rank == 0)
            {
                matrixA = InitializeByRandomNumbers(matrixARows, matrixAColumns);
                matrixB = InitializeByRandomNumbers(matrixBRows, matrixBColumns);
            }
            else
            {
                matrixA = Matrix2D<long>.CreateEmpty(matrixARows, matrixAColumns);
                matrixB = Matrix2D<long>.CreateEmpty(matrixBRows, matrixBColumns);
            }

            if (Communicator.world.Rank == 0)
            {
                double startNonParallel = MPI_Wtime();
                controlMatrix = matrixA.MultiplyBy(matrixB);
                double endNonParallel = MPI_Wtime();

                Console.WriteLine($"\nNon parallel blocking time: {endNonParallel - startNonParallel}");
            }

            //double startBlocking = MPI_Wtime();
            //var matrixC = matrixA.ClusteredMultiplyBy(matrixB);
            //double endBlocking = MPI_Wtime();

            //if (matrixC != null)
            //{
            //    Console.WriteLine($"\nParallel blocking time: {endBlocking - startBlocking}");
            //    Console.WriteLine($"Results equality: {CompareMatrices(controlMatrix, matrixC)}");
            //}

            //Communicator.world.Barrier();

            double startNonBlocking = MPI_Wtime();
            var matrixE = matrixA.ClusteredMultiplyByAsync(matrixB);
            double endNonBlocking = MPI_Wtime();

            if (matrixE != null)
            {
                Console.WriteLine($"\nParallel non blocking time: {endNonBlocking - startNonBlocking}");
                Console.WriteLine($"Results equality: {CompareMatrices(controlMatrix, matrixE)}");
            }
        }
    }
}
