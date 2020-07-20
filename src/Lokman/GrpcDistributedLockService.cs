using System;
using System.Threading.Tasks;
using Lokman.Protos;
using Grpc.Core;
using System.Threading;
using System.Linq;
using Google.Protobuf.WellKnownTypes;

namespace Lokman
{
    /// <summary>
    /// Grpc server-side service
    /// </summary>
    public class GrpcDistributedLockService : Protos.DistributedLockService.DistributedLockServiceBase
    {
        private readonly IEventLogger<GrpcDistributedLockService> _logger;
        private readonly IDistributedLockStore _store;

        public GrpcDistributedLockService(IEventLogger<GrpcDistributedLockService> logger, IDistributedLockStore lockStore)
        {
            _logger = logger;
            _store = lockStore;
        }

        public override Task<LockResponse> Lock(LockRequest request, ServerCallContext context)
            => ProcessAsync(request, context.CancellationToken);

        public override Task<LockInfoResponse> GetLockInfo(Empty request, ServerCallContext context)
            => GetLockInfoInternalAsync(context.CancellationToken);

        internal async Task<LockInfoResponse> GetLockInfoInternalAsync(CancellationToken cancellationToken)
        {
            var result = await _store.GetCurrentLocksAsync(cancellationToken).ConfigureAwait(false);
            var response = new LockInfoResponse();
            response.Locks.AddRange(result.Select(x => new Protos.LockInfo() {
                IsLocked = x.IsLocked,
                Expiration = Timestamp.FromDateTime(x.ExpirationUtc),
                Key = x.Key,
                Token = x.Token
            }));
            return response;
        }

        public async Task<LockResponse> ProcessAsync(LockRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                long token;
                var requestedDuration = request.Duration.ToTimeSpan();
                if (requestedDuration.Ticks == 0)
                {
                    _logger.DebugEvent("ProcessAsync.ReleaseAsync", new { Request = request });
                    token = await _store.ReleaseAsync(request.Key, request.Token, cancellationToken).ConfigureAwait(false);
                }
                else if (request.Token >= 0)
                {
                    _logger.DebugEvent("ProcessAsync.UpdateDurationAsync", new { Request = request });
                    token = await _store.UpdateAsync(request.Key, request.Token, requestedDuration, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    _logger.DebugEvent("ProcessAsync.AcquireAsync", new { Request = request });
                    token = await _store.AcquireAsync(request.Key, requestedDuration, cancellationToken).ConfigureAwait(false);
                }

                return new LockResponse() {
                    Key = request.Key,
                    Token = token,
                };
            }
            catch (Exception ex)
            {
                _logger.ErrorEvent(ex, "ProcessAsync.Error", new { Request = request, ErrorMsg = ex.Message, });
                return new LockResponse() {
                    Key = request.Key,
                    Token = -1,
                };
            }
        }
    }
}
