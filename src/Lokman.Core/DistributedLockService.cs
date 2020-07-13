using System;
using System.Threading.Tasks;
using Lokman.Protos;
using Grpc.Core;

namespace Lokman
{
    /// <summary>
    /// Grpc request processor
    /// </summary>
    public class DistributedLockService : Protos.DistributedLockService.DistributedLockServiceBase
    {
        private readonly IEventLogger<DistributedLockService> _logger;

        public DistributedLockService(IEventLogger<DistributedLockService> logger) => _logger = logger;

        public override async Task Lock(
            IAsyncStreamReader<LockRequest> requestStream,
            IServerStreamWriter<LockResponse> responseStream,
            ServerCallContext context)
        {
            try
            {
                await foreach (var request in requestStream.ReadAllAsync(context.CancellationToken))
                {
                }
            }
            catch (TaskCanceledException ex)
            {
                var now = DateTimeOffset.UtcNow;
                var ticks = now.UtcTicks;
                _logger.WarningEvent(ex, "LockRequestDropped", new { Ticks = ticks, context.Peer, });
                throw;
            }
        }

        public override Task<LockResponse> RestLock(LockRequest request, ServerCallContext context)
        {
            return Task.FromResult(new LockResponse() {
                Index = 222,
                Key = request.Key,
                Ticks = DateTimeOffset.UtcNow.UtcTicks,
            });
        }
    }
}
