using System;
using System.Threading.Tasks;
using Lokman.Protos;
using Grpc.Core;
using System.Threading;

namespace Lokman
{
    /// <summary>
    /// Grpc server-side service
    /// </summary>
    public class GrpcDistributedLockService : Protos.DistributedLockService.DistributedLockServiceBase
    {
        private readonly IEventLogger<GrpcDistributedLockService> _logger;
        private readonly IDistributedLockStore _lockStore;

        public GrpcDistributedLockService(IEventLogger<GrpcDistributedLockService> logger, IDistributedLockStore lockStore)
        {
            _logger = logger;
            _lockStore = lockStore;
        }

        public override Task<LockResponse> Lock(LockRequest request, ServerCallContext context)
            => ProcessAsync(request, context.CancellationToken);

        public async Task<LockResponse> ProcessAsync(LockRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                long token;
                var requestedDuration = request.Duration.ToTimeSpan();
                if (requestedDuration.Ticks == 0)
                {
                    _logger.DebugEvent("ProcessAsync.ReleaseAsync", new { Request = request });
                    token = await _lockStore.ReleaseAsync(request.Key, request.Token, cancellationToken).ConfigureAwait(false);
                }
                else if (request.Token >= 0)
                {
                    _logger.DebugEvent("ProcessAsync.UpdateDurationAsync", new { Request = request });
                    token = await _lockStore.UpdateAsync(request.Key, request.Token, requestedDuration, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    _logger.DebugEvent("ProcessAsync.AcquireAsync", new { Request = request });
                    token = await _lockStore.AcquireAsync(request.Key, requestedDuration, cancellationToken).ConfigureAwait(false);
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
