using System;
using System.Runtime.InteropServices;


namespace Matrices.Shared.Unsafe
{
    using MPI_Comm = Int32;
    using MPI_Datatype = Int32;
    using MPI_Request = Int32;

    public unsafe class MpiUnsafe
    {
        private const string MPI_DLL = "msmpi.dll";

        public const MPI_Request MPI_REQUEST_NULL = 0x2c000000;

        public const Int32 MPI_LONG_LONG = 0x4c000809;
        public const Int32 MPI_COMM_WORLD = 0x44000000;

        [DllImport(MPI_DLL, CallingConvention = CallingConvention.StdCall)]
        public static unsafe extern int MPI_Scatterv(IntPtr sendbuf, int[] sendcounts, int[] displs, MPI_Datatype sendtype,
                                                     IntPtr recvbuf, int recvcount, MPI_Datatype recvtype, int root, MPI_Comm comm);


        [DllImport(MPI_DLL, CallingConvention = CallingConvention.StdCall)]
        public static unsafe extern int MPI_Iscatterv(IntPtr sendbuf, int[] sendcounts, int[] displs, MPI_Datatype sendtype,
                                                     IntPtr recvbuf, int recvcount, MPI_Datatype recvtype, int root, MPI_Comm comm, out MPI_Request request);

    }
}
