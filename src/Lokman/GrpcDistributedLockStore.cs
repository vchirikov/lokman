using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
        private static readonly Empty _emptyRequest = new Empty();
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

        /// <inheritdoc />
        public async ValueTask<IReadOnlyCollection<LockInfo>> GetCurrentLocksAsync(CancellationToken cancellationToken = default)
        {
            var response = await _grpc.GetLockInfoAsync(_emptyRequest, cancellationToken: cancellationToken).ConfigureAwait(false);
            return response.Locks.Select(l => new LockInfo(l.Key, l.IsLocked, l.Token, l.Expiration.ToDateTime())).ToList();
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
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