using System;
using System.Collections.Generic;
using System.Linq;
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

        public static void MultiplyInGroups(Matrix2D<long> matrixA, Matrix2D<long> matrixB, int groups)
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
            Matrix2D<long> matrixC = matrixA.GroupedMultiplyBy(matrixB, groupCommunicator);
            double endGroup = MPI_Wtime();

            if (matrixC != null)
            {
                double startNonParallel = MPI_Wtime();
                Matrix2D<long> matrixD = matrixA.MultiplyBy(matrixB);
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

        public static Matrix2D<long> GroupedMultiplyBy(this Matrix2D<long> self,
            Matrix2D<long> multiplier, Intracommunicator communicator)
        {
            int size = communicator.Size;
            int rank = communicator.Rank;

            int resultSize = self.Rows * multiplier.Columns;
            int[] counts = Arrays.equalPartLengths(resultSize, size).ToArray();
            var (firstIndex, lastIndex) = Arrays.getPartIndicesRange(counts, rank);

            long[] localResult = MatrixDivisionService.MultiplyFrame(
                firstIndex, lastIndex, multiplier.Columns,
                self, multiplier).ToArray();

            if (rank == MasterRank)
            {
                long[] globalResult = communicator.GatherFlattened(localResult, counts, MasterRank);
                Matrix2D<long> result = Matrix2D<long>.CreateEmpty(self.Rows, multiplier.Columns);
                result.CommitFrame(0, resultSize - 1, globalResult);

                return result;
            }

            communicator.GatherFlattened(localResult, counts, MasterRank);

            return null;
        }
    }
}
