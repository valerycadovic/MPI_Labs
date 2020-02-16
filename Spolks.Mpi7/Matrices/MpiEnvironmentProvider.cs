using System;
using MpiEnvironment = MPI.Environment;

namespace Matrices
{
    public class MpiEnvironmentProvider
    {
        private static readonly Lazy<MpiEnvironment> LazyInstance;

        public static MpiEnvironment MpiEnvironment => LazyInstance.Value;

        static MpiEnvironmentProvider()
        {
            LazyInstance = new Lazy<MpiEnvironment>(() =>
            {
                string[] args = { };
                return new MpiEnvironment(ref args);
            });
        }

        private MpiEnvironmentProvider()
        {
        }

        ~MpiEnvironmentProvider()
        {
            LazyInstance.Value.Dispose();
        }
    }
}
