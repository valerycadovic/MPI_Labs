using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Matrices.Shared;
using Matrices.Shared.Services;
using Matrices.Shared.Unsafe;
using MPI;

namespace Matrices.Mpi7.Services
{
    public static class MatrixClusteringService
    {
        private const int MasterRank = 0;

        public unsafe static Matrix2D<long> ClusteredMultiplyByAsync(this Matrix2D<long> self,
            Matrix2D<long> multiplier, int rowsPerCycle)
        {
            var communicator = Communicator.world;
            Unsafe.MPI_Comm_rank(MpiUnsafe.MPI_COMM_WORLD, out var rank);
            Unsafe.MPI_Comm_size(MpiUnsafe.MPI_COMM_WORLD, out var size);

            var globalCounts = Arrays.equalPartLengths(self.Rows * multiplier.Columns, size);
            var (firstIndex, lastIndex) = Arrays.getPartIndicesRange(globalCounts, rank);

            communicator.Broadcast(ref multiplier, MasterRank);

            var selfMatrix = self.ToArray();
            var calculationCycles = (lastIndex - firstIndex + 1) / self.Columns / rowsPerCycle;

            var counts = Enumerable.Range(0, size).Select(_ => (lastIndex - firstIndex + 1) / calculationCycles).ToArray();
            var period = rowsPerCycle * self.Columns;
            var displacement = Enumerable.Range(0, size).Select((_, index) => globalCounts.Take(index).Sum()).ToArray();

            var buffer0 = (long[]) GCHandle.Alloc(new long[counts[rank]], GCHandleType.Pinned).Target;
            var buffer1 = (long[]) GCHandle.Alloc(new long[counts[rank]], GCHandleType.Pinned).Target;

            MpiUnsafe.MPI_Scatterv(
                Marshal.UnsafeAddrOfPinnedArrayElement(selfMatrix, 0),
                counts,
                displacement,
                MpiUnsafe.MPI_LONG_LONG,
                Marshal.UnsafeAddrOfPinnedArrayElement(buffer0, 0),
                buffer0.Length,
                MpiUnsafe.MPI_LONG_LONG,
                MasterRank,
                MpiUnsafe.MPI_COMM_WORLD
            );

            var results = new List<IEnumerable<long>>();
            var requestsList = new List<int>();

            for (int i = 0; i < calculationCycles; i++)
            {
                var first = displacement[rank];
                var last = displacement[rank] + counts[rank] - 1;

                if (i != calculationCycles - 1)
                {
                    displacement = displacement.Zip(counts).Select(tuple => tuple.First + tuple.Second).ToArray();

                    var errorCode = MpiUnsafe.MPI_Iscatterv(
                        Marshal.UnsafeAddrOfPinnedArrayElement(selfMatrix, 0),
                        counts,
                        displacement,
                        MpiUnsafe.MPI_LONG_LONG,
                        Marshal.UnsafeAddrOfPinnedArrayElement((i + 1) % 2 == 1 ? buffer1 : buffer0, 0),
                        buffer0.Length,
                        MpiUnsafe.MPI_LONG_LONG,
                        MasterRank,
                        MpiUnsafe.MPI_COMM_WORLD,
                        out int request
                    );

                    requestsList.Add(request);
                }

                if (i != 0)
                {
                    var request = requestsList[i - 1];

                    Unsafe.MPI_Wait(ref request, out var status);
                }

                self.CommitFrame(first, last, i % 2 == 1 ? buffer1 : buffer0);

                var localResult = MatrixDivisionService.MultiplyFrame(first, last, multiplier.Columns, self, multiplier).ToArray();
                results.Add(localResult);
            }

            var globalResult = communicator.GatherFlattened(results.SelectMany(e => e).ToArray(), globalCounts, MasterRank);
            if (globalResult == null)
            {
                return null;
            }

            var result = Matrix2D<long>.CreateEmpty(self.Rows, multiplier.Columns);
            result.CommitFrame(0, result.Size - 1, globalResult);

            return result;
        }

        public static Matrix2D<long> ClusteredMultiplyBy(
            this Matrix2D<long> self, Matrix2D<long> multiplier)
        {
            var communicator = Communicator.world;

            Unsafe.MPI_Comm_rank(MpiUnsafe.MPI_COMM_WORLD, out var rank);
            Unsafe.MPI_Comm_size(MpiUnsafe.MPI_COMM_WORLD, out var size);

            communicator.Broadcast(ref multiplier, MasterRank);

            var counts = Arrays.equalPartLengths(self.Rows * multiplier.Columns, size);
            var (firstIndex, lastIndex) = Arrays.getPartIndicesRange(counts, rank);

            var selfMatrix = self.ToArray();
            var scatterResult = (long[]) GCHandle.Alloc(new long[counts[rank]], GCHandleType.Pinned).Target;

            var displacement = Enumerable.Range(0, size).Select((_, index) => counts.Take(index).Sum()).ToArray();

            MpiUnsafe.MPI_Scatterv(
                Marshal.UnsafeAddrOfPinnedArrayElement(selfMatrix, 0),
                counts,
                displacement,
                MpiUnsafe.MPI_LONG_LONG,
                Marshal.UnsafeAddrOfPinnedArrayElement(scatterResult, 0),
                counts[rank],
                MpiUnsafe.MPI_LONG_LONG,
                MasterRank,
                MpiUnsafe.MPI_COMM_WORLD
            );

            self.CommitFrame(firstIndex, lastIndex, scatterResult);

            var localResult = MatrixDivisionService.MultiplyFramе(firstIndex, lastIndex, multiplier.Columns, self, multiplier).ToArray();

            var globalResult = new long[self.Rows * multiplier.Columns];

            Unsafe.MPI_Gatherv(
                Marshal.UnsafeAddrOfPinnedArrayElement(localResult, 0),
                localResult.Length,
                MpiUnsafe.MPI_LONG_LONG,
                Marshal.UnsafeAddrOfPinnedArrayElement(globalResult, 0),
                counts,
                displacement,
                MpiUnsafe.MPI_LONG_LONG,
                MasterRank,
                MpiUnsafe.MPI_COMM_WORLD
            );

            if (rank != MasterRank)
            {
                return null;
            }

            var result = Matrix2D<long>.CreateEmpty(self.Rows, multiplier.Columns);
            result.CommitFrame(0, result.Size - 1, globalResult);

            return result;
        }
    }
}
