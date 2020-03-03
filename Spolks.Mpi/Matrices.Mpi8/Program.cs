using System;
using System.Linq;
using FilesMultiplication;
using Matrices.Mpi8.Services;
using Matrices.Shared;
using Matrices.Shared.Services;
using MpiEnvironment = MPI.Environment;

namespace Matrices.Mpi8
{
    class Program
    {
        static void Main(string[] args)
        {
            int matrixN = int.Parse(args[0]);
            int groups = int.Parse(args[1]);

            if (!args.Contains("-f"))
            {
                var matrixA = MatrixService.InitializeByNaturalNumbers(matrixN, matrixN);
                var matrixB = MatrixService.InitializeByNaturalNumbers(matrixN, matrixN);

                using var env = new MpiEnvironment(ref args);

                MatrixGroupingClusteringService.MultiplyInGroups(matrixA, matrixB, groups);
            }
            else
            {
                const string path = @"C:\";

                using var env = new MpiEnvironment(ref args);

                var fileOperations = new FileOperations(path, path, path, groups);

                Matrix2D<long> Fill() => MatrixService.InitializeByRandomNumbers(matrixN, matrixN);

                fileOperations.Fill(Fill, Fill);
                fileOperations.Multiply();
            }
        }
    }
}
