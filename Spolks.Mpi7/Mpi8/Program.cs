using System;
using System.Linq;
using System.Text;
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

            Matrix2D<long> matrixA = MatrixService.InitializeByNaturalNumbers(300, 300);
            Matrix2D<long> matrixB = MatrixService.InitializeByNaturalNumbers(300, 300);

            const int groups = 2;

            MatrixGroupingClusteringService.MultiplyInGroups(matrixA, matrixB, groups);
        }

        static int[] GetCounts(int[] array, int size)
        {
            var division = DivideWithRemainder(array.Length, size);

            return Enumerable.Repeat(division.result, size - 1).Append(division.result + division.remainder).ToArray();
        }

        static (int result, int remainder) DivideWithRemainder(int a, int b)
        {
            return (a / b, a % b);
        }

        static void Print(int[] numbers, int rank)
        {
            var sb = new StringBuilder();
            sb.Append($"Rank {rank}: ");

            foreach (var number in numbers)
            {
                sb.Append($"{number} ");
            }

            Console.WriteLine(sb.ToString());
        }
    }
}
