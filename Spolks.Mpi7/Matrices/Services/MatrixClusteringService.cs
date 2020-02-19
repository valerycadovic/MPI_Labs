using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Matrices.Shared;
using Matrices.Shared.Services;
using MPI;
using static Matrices.Shared.Services.MatrixDivisionService;

namespace Matrices.Mpi7.Services
{
    public static class MatrixClusteringService
    {
        private const int MasterRank = 0;
        private const int Tag = 10;

        public static Matrix2D<BigInteger> ClusteredMultiplyByAsync(this Matrix2D<BigInteger> self,
            Matrix2D<BigInteger> multiplier)
        {
            Communicator communicator = Communicator.world;
            int size = communicator.Size;
            int rank = communicator.Rank;

            var result = Matrix2D<BigInteger>.CreateEmpty(self.Rows, multiplier.Columns);
            var rankRange = result.GetFrameIndexes(rank, size);

            if (rank == MasterRank)
            {
                var requestList = new RequestList();
                var requests = new List<ReceiveRequest>();

                for (int i = size - 1; i > 0; i--)
                {
                    ReceiveRequest request = communicator.ImmediateReceive<MatrixFrame>(source: i, Tag);
                    requestList.Add(request);
                    requests.Add(request);
                }

                var rankResults = MultiplyFrame(rankRange.first, rankRange.last, result.Columns, self, multiplier);
                result.CommitFrame(rankRange.first, rankRange.last, rankResults.ToArray());

                requestList.WaitAll();

                foreach (var resultFrame in requests.Select(f => (MatrixFrame)f.GetValue()))
                {
                    result.CommitFrame(resultFrame.First, resultFrame.Last, resultFrame.Results);
                }

                return result;
            }

            var results = MultiplyFrame(rankRange.first, rankRange.last, result.Columns, self, multiplier);
            var frame = new MatrixFrame(rankRange.first, rankRange.last, results.ToArray());
            var sendRequest = communicator.ImmediateSend(frame, MasterRank, Tag);
            sendRequest.Wait();

            return null;
        }

        public static Matrix2D<BigInteger> ClusteredMultiplyBy(
            this Matrix2D<BigInteger> self, Matrix2D<BigInteger> multiplier)
        {
            Communicator communicator = Communicator.world;
            int size = communicator.Size;
            int rank = communicator.Rank;

            var result = Matrix2D<BigInteger>.CreateEmpty(self.Rows, multiplier.Columns);
            var rankRange = result.GetFrameIndexes(rank, size);
            var rankResults = MultiplyFrame(rankRange.first, rankRange.last, result.Columns, self, multiplier);

            if (rank == MasterRank)
            {
                result.CommitFrame(rankRange.first, rankRange.last, rankResults.ToArray());

                for (int i = size - 1; i > 0; i--)
                {
                    communicator.Receive(Unsafe.MPI_ANY_SOURCE, Tag, out MatrixFrame outFrame);
                    result.CommitFrame(outFrame.First, outFrame.Last, outFrame.Results);
                }

                return result;
            }

            var frame = new MatrixFrame(rankRange.first, rankRange.last, rankResults.ToArray());
            communicator.Send(frame, MasterRank, Tag);

            return null;
        }
    }
}
