using System;
using Matrices.Mpi7.Services;
using Matrices.Shared;
using Matrices.Shared.Services;
using MPI;
using MpiEnvironment = MPI.Environment;
using static MPI.Unsafe;
using static Matrices.Shared.Services.MatrixService;
using System.Linq;
using Matrices.Shared.Unsafe;

namespace Matrices.Mpi7
{
    unsafe class Program
    {
        static unsafe void Main(string[] args)
        {
            int matrixARows = int.Parse(args[0]);
            int matrixAColumns = int.Parse(args[1]);
            int matrixBRows = matrixAColumns;
            int matrixBColumns = int.Parse(args[2]);
            int rowsPerCycle = int.Parse(args[3]);

            string[] mpiArgs = { };
            using var environment = new MpiEnvironment(ref mpiArgs);

            Matrix2D<long> matrixA;
            Matrix2D<long> matrixB;
            Matrix2D<long> controlMatrix = Matrix2D<long>.CreateEmpty(matrixARows, matrixAColumns);

            if (Communicator.world.Rank == 0)
            {
                matrixA = InitializeByNaturalNumbers(matrixARows, matrixAColumns);
                matrixB = InitializeByNaturalNumbers(matrixBRows, matrixBColumns);
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

            Unsafe.MPI_Barrier(MpiUnsafe.MPI_COMM_WORLD);

            {
                var localMatrixA = Matrix2D<long>.FromArray(matrixA.ToArray(), matrixA.Rows, matrixA.Columns);
                var localMatrixB = Matrix2D<long>.FromArray(matrixB.ToArray(), matrixB.Rows, matrixB.Columns);

                double startBlocking = MPI_Wtime();
                var result = localMatrixA.ClusteredMultiplyBy(localMatrixB);
                double endBlocking = MPI_Wtime();

                if (Communicator.world.Rank == 0)
                {
                    Console.WriteLine($"\nParallel blocking time: {endBlocking - startBlocking}");
                    Console.WriteLine($"Results equality: {CompareMatrices(controlMatrix, result)}");
                }
            }

            Communicator.world.Barrier();
            
            try
            {
                var localMatrixA = Matrix2D<long>.FromArray(matrixA.ToArray(), matrixA.Rows, matrixA.Columns);
                var localMatrixB = Matrix2D<long>.FromArray(matrixB.ToArray(), matrixB.Rows, matrixB.Columns);

                double startNonBlocking = MPI_Wtime();
                var result = localMatrixA.ClusteredMultiplyByAsync(localMatrixB, rowsPerCycle);
                double endNonBlocking = MPI_Wtime();

                if (result != null)
                {
                    Console.WriteLine($"\nParallel non blocking time: {endNonBlocking - startNonBlocking}");
                    Console.WriteLine($"Results equality: {CompareMatrices(controlMatrix, result)}");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"{Communicator.world.Rank}: {e.Message}");
            }
        }
    }
}
