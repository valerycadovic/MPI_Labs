using Matrices.Mpi8.Services;
using Matrices.Shared.Services;
using MpiEnvironment = MPI.Environment;

namespace Matrices.Mpi8
{
    class Program
    {
        static void Main(string[] args)
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
    }
}
