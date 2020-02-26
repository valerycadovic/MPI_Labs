using System.Collections.Generic;
using System.Linq;
using Matrices.Shared;
using Matrices.Shared.Services;
using MPI;
using static Matrices.Shared.Services.MatrixDivisionService;

namespace Matrices.Mpi7.Services
{
    public static class MatrixClusteringService
    {
        private const int MasterRank = 0;

        public static Matrix2D<long> ClusteredMultiplyByAsync(this Matrix2D<long> self,
            Matrix2D<long> multiplier)
        {
            var communicator = Communicator.world;
            int size = communicator.Size;
            int rank = communicator.Rank;

            var counts = Arrays.equalPartLengths(self.Rows * multiplier.Columns, size);
            var (firstIndex, lastIndex) = Arrays.getPartIndicesRange(counts, rank);

            communicator.Broadcast(ref multiplier, MasterRank);

            var result = Matrix2D<long>.CreateEmpty(self.Rows, multiplier.Columns);

            if (rank == MasterRank)
            {
                var requestList = new RequestList();
                var requests = new List<ReceiveRequest>();

                for (int i = 0; i < size; i++)
                {
                    if (i == MasterRank)
                    {
                        continue;
                    }

                    var (firstRankIndex, lastRankIndex) = Arrays.getPartIndicesRange(counts, i);

                    for (var currentIndex = firstRankIndex; currentIndex < lastRankIndex; currentIndex += result.Columns)
                    {
                        ReceiveRequest request = communicator.ImmediateReceive<MatrixFrame>(source: i, currentIndex);

                        requestList.Add(request);
                        requests.Add(request);
                    }
                }

                var rankResults = MultiplyFrame(firstIndex, lastIndex, result.Columns, self, multiplier);
                result.CommitFrame(firstIndex, lastIndex, rankResults.ToArray());

                requestList.WaitAll();

                foreach (var resultFrame in requests.Select(f => (MatrixFrame)f.GetValue()))
                {
                    result.CommitFrame(resultFrame.First, resultFrame.Last, resultFrame.Results);
                }

                return result;
            }
            else
            {
                var requestList = new RequestList();

                for (var currentIndex = firstIndex; currentIndex < lastIndex; currentIndex += result.Columns)
                {
                    var first = currentIndex;
                    var last = currentIndex + result.Columns - 1;

                    var currentMultiplicationResults = MultiplyFrame(first, last, result.Columns, self, multiplier);
                    var frame = new MatrixFrame(first, last, currentMultiplicationResults.ToArray());

                    var sendRequest = communicator.ImmediateSend(frame, MasterRank, currentIndex);
                    requestList.Add(sendRequest);
                }

                requestList.WaitAll();

                return null;
            }
        }

        public static Matrix2D<long> ClusteredMultiplyBy(
            this Matrix2D<long> self, Matrix2D<long> multiplier)
        {
            var communicator = Communicator.world;
            int size = communicator.Size;
            int rank = communicator.Rank;

            communicator.Broadcast(ref multiplier, MasterRank);

            var counts = Arrays.equalPartLengths(self.Rows * multiplier.Columns, size);
            var (firstIndex, lastIndex) = Arrays.getPartIndicesRange(counts, rank);

            var scatterResult = communicator.ScatterFromFlattened(self.ToArray(), counts, MasterRank);
            self.CommitFrame(firstIndex, lastIndex, scatterResult);

            var localResult = MultiplyFrame(firstIndex, lastIndex, multiplier.Columns, self, multiplier).ToArray();

            if (rank == MasterRank)
            {
                var globalResult = communicator.GatherFlattened(localResult, counts, MasterRank);
                
                var result = Matrix2D<long>.CreateEmpty(self.Rows, multiplier.Columns);
                result.CommitFrame(0, result.Size - 1, globalResult);

                return result;
            }

            communicator.GatherFlattened(localResult, counts, MasterRank);

            return null;
        }
    }
}
