using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Lokman.Protos;

namespace Lokman
{
    /// <summary>
    /// Remote <see cref="IDistributedLockStore"/> implementation
    /// </summary>
    public class GrpcDistributedLockStore : IDistributedLockStore
    {
        private readonly DistributedLockService.DistributedLockServiceClient _grpc;

        public GrpcDistributedLockStore(DistributedLockService.DistributedLockServiceClient grpc) => _grpc = grpc;

        public async ValueTask<long> AcquireAsync(string key, TimeSpan duration, CancellationToken cancellationToken = default)
        {
            var request = new LockRequest() {
                Key = key,
                Duration = Duration.FromTimeSpan(duration),
                Token = -1,
            };
            var response = await _grpc.LockAsync(request, cancellationToken: cancellationToken).ConfigureAwait(false);
            return response.Token;
        }

        public async ValueTask<long> ReleaseAsync(string key, long token, CancellationToken cancellationToken = default)
        {
            var request = new LockRequest() {
                Key = key,
                Duration = Duration.FromTimeSpan(TimeSpan.Zero),
                Token = token,
            };
            var response = await _grpc.LockAsync(request, cancellationToken: cancellationToken).ConfigureAwait(false);
            return response.Token;
        }

        public async ValueTask<long> UpdateAsync(string key, long token, TimeSpan duration, CancellationToken cancellationToken = default)
        {
            var request = new LockRequest() {
                Key = key,
                Duration = Duration.FromTimeSpan(duration),
                Token = token,
            };
            var response = await _grpc.LockAsync(request, cancellationToken: cancellationToken).ConfigureAwait(false);
            return response.Token;
        }
    }
}