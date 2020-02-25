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
            using var env = new MpiEnvironment(ref args);

            int matrixARows = int.Parse(args[0]);
            int matrixAColumns = int.Parse(args[1]);
            int matrixBRows = matrixAColumns;
            int matrixBColumns = int.Parse(args[2]);
            int groups = int.Parse(args[3]);

            if (!args.Contains("-f"))
            {
                var matrixA = MatrixService.InitializeByNaturalNumbers(matrixARows, matrixAColumns);
                var matrixB = MatrixService.InitializeByNaturalNumbers(matrixBRows, matrixBColumns);

                MatrixGroupingClusteringService.MultiplyInGroups(matrixA, matrixB, groups);
            }
            else
            {
                string path = args[4];
                var fileOperations = new FileOperations(path, path, path, groups);

                Matrix2D<long> FillA() => MatrixService.InitializeByNaturalNumbers(matrixARows, matrixAColumns);
                Matrix2D<long> FillB() => MatrixService.InitializeByNaturalNumbers(matrixBRows, matrixBColumns);

                fileOperations.Fill(FillA, FillB);
                fileOperations.Multiply(); 
                bool comparison = fileOperations.Compare();

                Console.WriteLine($"Multiplication result: {comparison}");
            }
        }
    }
}
