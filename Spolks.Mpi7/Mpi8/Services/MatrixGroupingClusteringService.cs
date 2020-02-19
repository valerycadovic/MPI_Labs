using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Matrices.Shared;
using Matrices.Shared.Services;
using MPI;
using Matrices.Shared.Extensions;
using static Matrices.Shared.Services.MatrixService;
using static MPI.Unsafe;

namespace Matrices.Mpi8.Services
{
    public static class MatrixGroupingClusteringService
    {
        private const int MasterRank = 0;

        public static void MultiplyInGroups(Matrix2D<BigInteger> matrixA, Matrix2D<BigInteger> matrixB, int groups)
        {
            if (groups <= 0)
            {
                throw new ArgumentOutOfRangeException($"{nameof(groups)} shoud be an integer positive number");
            }

            Intracommunicator world = Communicator.world;
            var groupsPerRanks = CreateGroups(world, groups);

            var currentGroup = groupsPerRanks[world.Rank];
            var groupCommunicator = (Intracommunicator)world.Create(currentGroup.group);

            double startGroup = MPI_Wtime();
            Matrix2D<BigInteger> matrixC = matrixA.GroupedMultiplyBy(matrixB, groupCommunicator);
            double endGroup = MPI_Wtime();

            if (matrixC != null)
            {
                double startNonParallel = MPI_Wtime();
                Matrix2D<BigInteger> matrixD = matrixA.MultiplyBy(matrixB);
                double endNonParallel = MPI_Wtime();

                if (world.Rank == MasterRank)
                {
                    Console.WriteLine($"Non parallel execution time: {endNonParallel - startNonParallel}");
                }

                Console.WriteLine($"Group #{currentGroup.id} execution time: {endGroup - startGroup}");
                Console.WriteLine($"Group #{currentGroup.id} matrices comparison: {CompareMatrices(matrixC, matrixD)}");
            }
        }

        private static Dictionary<int, (int id, Group group)> CreateGroups(
            Intracommunicator communicator, int groups)
        {
            var groupsRanks = Enumerable.Range(0, communicator.Size).ToArray().SplitToDictionary(groups);
            var groupsPerRanks = new Dictionary<int, (int, Group)>();

            foreach (var (groupId, groupRanks) in groupsRanks)
            {
                Group group = communicator.Group.IncludeOnly(groupRanks);
                foreach (var rank in groupRanks)
                {
                    groupsPerRanks.Add(rank, (groupId, group));
                }
            }

            return groupsPerRanks;
        }

        public static Matrix2D<BigInteger> GroupedMultiplyBy(this Matrix2D<BigInteger> self,
            Matrix2D<BigInteger> multiplier, Intracommunicator communicator)
        {
            int size = communicator.Size;
            int rank = communicator.Rank;

            int resultSize = self.Rows * multiplier.Columns;
            var frameRange = MatrixDivisionService.GetFrameIndexes(resultSize, rank, size);
            int[] counts = GetCounts(resultSize, size);

            BigInteger[] localResult = MatrixDivisionService.MultiplyFrame(
                frameRange.first, frameRange.last, multiplier.Columns,
                self, multiplier).ToArray();

            if (rank == MasterRank)
            {
                BigInteger[] globalResult = communicator.GatherFlattened(localResult, counts, MasterRank);
                Matrix2D<BigInteger> result = Matrix2D<BigInteger>.CreateEmpty(self.Rows, multiplier.Columns);
                result.CommitFrame(0, resultSize - 1, globalResult);

                return result;
            }

            communicator.GatherFlattened(localResult, counts, MasterRank);

            return null;
        }

        private static int[] GetCounts(int matrixSize, int communicatorSize)
        {
            var division = matrixSize.DivideWithRemainder(communicatorSize);

            return Enumerable
                .Repeat(division.result, communicatorSize - 1)
                .Append(division.result + division.remainder)
                .ToArray();
        }
    }
}
