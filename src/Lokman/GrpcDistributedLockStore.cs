using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;

namespace Lokman
{
    /// <summary>
    /// Remote <see cref="IDistributedLockStore"/> implementation
    /// </summary>
    public class GrpcDistributedLockStore : IDistributedLockStore
    {
        private readonly Protos.DistributedLockService.DistributedLockServiceClient _grpc;

        public GrpcDistributedLockStore(Protos.DistributedLockService.DistributedLockServiceClient grpc) => _grpc = grpc;

        public ValueTask<Epoch> AcquireAsync(string key, long expiration, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public ValueTask<Epoch> ReleaseAsync(string key, long index, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public ValueTask<Epoch> SetExpirationAsync(string key, long index, long expiration, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}