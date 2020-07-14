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
    public class DistributedLockService : Protos.DistributedLockService.DistributedLockServiceBase, IAsyncDisposable, IDisposable
    {
        private readonly IEventLogger<DistributedLockService> _logger;
        private readonly IDistributedLockStore _lockStore;
        private readonly ITime _time;
        private bool _isDisposed;

        public DistributedLockService(IEventLogger<DistributedLockService> logger, IDistributedLockStore lockStore) : this(logger, lockStore, SystemTime.Instance) { }

        internal DistributedLockService(IEventLogger<DistributedLockService> logger, IDistributedLockStore lockStore, ITime time)
        {
            _logger = logger;
            _lockStore = lockStore;
            _time = time;
        }

        public override Task<LockResponse> Lock(LockRequest request, ServerCallContext context)
            => ProcessAsync(request, context.CancellationToken);

        public async Task<LockResponse> ProcessAsync(LockRequest request, CancellationToken cancellationToken = default)
        {
            var currentTimeTicks = _time.UtcNow.Ticks;
            try
            {
                Epoch epoch;
                if (request.Expiration == 0 || request.Expiration <= currentTimeTicks)
                {
                    _logger.DebugEvent("ProcessAsync.ReleaseAsync", new { Request = request });
                    epoch = await _lockStore.ReleaseAsync(request.Key, request.Index, cancellationToken).ConfigureAwait(false);
                }
                else if (request.Index >= 0)
                {
                    _logger.DebugEvent("ProcessAsync.SetExpirationAsync", new { Request = request });
                    epoch = await _lockStore.SetExpirationAsync(request.Key, request.Index, request.Expiration, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    _logger.DebugEvent("ProcessAsync.AcquireAsync", new { Request = request });
                    epoch = await _lockStore.AcquireAsync(request.Key, request.Expiration, cancellationToken).ConfigureAwait(false);
                }

                return new LockResponse() {
                    Key = request.Key,
                    Index = epoch.Index,
                    Ticks = epoch.Ticks,
                };
            }
            catch (Exception ex)
            {
                _logger.ErrorEvent(ex, "ProcessAsync.Error", new { Request = request, ErrorMsg = ex.Message, });
                return new LockResponse() {
                    Key = request.Key,
                    Index = -1,
                    Ticks = _time.UtcNow.Ticks,
                };
            }
        }

        protected virtual async ValueTask DisposeAsyncCore()
        {
            if (_lockStore is IAsyncDisposable asyncDisposable1)
                await asyncDisposable1.DisposeAsync().ConfigureAwait(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    (_lockStore as IDisposable)?.Dispose();
                }
                _isDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore().ConfigureAwait(false);
            Dispose(disposing: false);
            GC.SuppressFinalize(this);
        }
    }
}
