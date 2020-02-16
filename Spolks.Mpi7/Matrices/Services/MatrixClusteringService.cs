using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using MPI;

namespace Matrices.Services
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
            var rankRange = result.GetRankIndexes(rank, size);

            if (rank == MasterRank)
            {
                var requestList = new RequestList();
                var frames = new List<Frame>();

                for (int i = size - 1; i > 0; i--)
                {
                    ReceiveRequest request = communicator.ImmediateReceive<Frame>(source: i, Tag);
                    requestList.Add(request);
                    frames.Add((Frame)request.GetValue());
                }

                var rankResults = MultiplyRank(rankRange.first, rankRange.last, result.Columns, self, multiplier);
                CommitFrame(result, rankRange.first, rankRange.last, rankResults.ToArray());

                requestList.WaitAll();

                foreach (var resultFrame in frames)
                {
                    CommitFrame(result, resultFrame.First, resultFrame.Last, resultFrame.Results);
                }

                return result;
            }

            var results = MultiplyRank(rankRange.first, rankRange.last, result.Columns, self, multiplier);
            var frame = new Frame(rankRange.first, rankRange.last, results.ToArray());
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
            var rankRange = result.GetRankIndexes(rank, size);
            var rankResults = MultiplyRank(rankRange.first, rankRange.last, result.Columns, self, multiplier);

            if (rank == MasterRank)
            {
                CommitFrame(result, rankRange.first, rankRange.last, rankResults.ToArray());

                for (int i = size - 1; i > 0; i--)
                {
                    communicator.Receive(Unsafe.MPI_ANY_SOURCE, Tag, out Frame outFrame);
                    CommitFrame(result, outFrame.First, outFrame.Last, outFrame.Results);
                }

                return result;
            }

            var frame = new Frame(rankRange.first, rankRange.last, rankResults.ToArray());
            communicator.Send(frame, MasterRank, Tag);

            return null;
        }

        private static void CommitFrame(Matrix2D<BigInteger> result, int first, int last, BigInteger[] frame)
        {
            for (int i = first, j = 0; i <= last; i++, j++)
            {
                result[i] = frame[j];
            }
        }

        private static (int first, int last) GetRankIndexes(this Matrix2D<BigInteger> matrix, int rank, int size)
        {
            int frameSize = matrix.Size / size;
            int first = rank * frameSize;
            int last = rank == size - 1 ? matrix.Size - 1 : first + frameSize - 1;

            return (first, last);
        }

        private static IEnumerable<BigInteger> MultiplyRank(
            int first, int last, int columns, Matrix2D<BigInteger> a, Matrix2D<BigInteger> b)
        {
            for (int absoluteIndex = first; absoluteIndex <= last; absoluteIndex++)
            {
                int i = absoluteIndex / columns;
                int j = absoluteIndex % columns;

                yield return a.GetRow(i)
                    .Zip(b.GetColumn(j))
                    .Select(tuple => tuple.First * tuple.Second)
                    .Aggregate((f, s) => f + s);
            }
        }

        [Serializable]
        private class Frame
        {
            public int First { get; }

            public int Last { get; }

            public BigInteger[] Results { get; }

            public Frame(int first, int last, BigInteger[] results)
            {
                First = first;
                Last = last;
                Results = results;
            }
        }
    }
}
