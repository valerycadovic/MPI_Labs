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
            if (!args.Contains("-f"))
            {
                int matrixARows = int.Parse(args[0]);
                int matrixAColumns = int.Parse(args[1]);
                int matrixBRows = matrixAColumns;
                int matrixBColumns = int.Parse(args[2]);
                int groups = int.Parse(args[3]);

                var matrixA = MatrixService.InitializeByNaturalNumbers(matrixARows, matrixAColumns);
                var matrixB = MatrixService.InitializeByNaturalNumbers(matrixBRows, matrixBColumns);

                using var env = new MpiEnvironment(ref args);

                MatrixGroupingClusteringService.MultiplyInGroups(matrixA, matrixB, groups);
            }
            else
            {
                const string path = @"C:\";

                using var env = new MpiEnvironment(ref args);

                var fileOperations = new FileOperations(path, path, path, 1);

                static Matrix2D<long> Fill() => MatrixService.InitializeByNaturalNumbers(100, 100);

                fileOperations.Fill(Fill, Fill);
                fileOperations.Multiply();
                bool comparison = fileOperations.Compare();

                Console.WriteLine($"Multiplication result: {comparison}");
            }
        }
    }
}
