using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lokman
{
    public interface IDistributedLockStore
    {
        ValueTask<Epoch> AcquireAsync(string key, long expiration, CancellationToken cancellationToken = default);
        ValueTask<Epoch> SetExpirationAsync(string key, long index, long expiration, CancellationToken cancellationToken = default);
        ValueTask<Epoch> ReleaseAsync(string key, long index, CancellationToken cancellationToken = default);
    }

    public class DistributedLockStore : IDistributedLockStore, IDisposable
    {
        /// <summary>
        /// Increasing store state counter
        /// </summary>
        private long _epoch;
        private bool _isDisposed;
        private readonly ITime _time;
        private readonly IDistributedLockStoreCleanupStrategy _cleanupStrategy;
        private readonly IExpirationQueue _expirationQueue;

        internal readonly ConcurrentDictionary<string, SemaphoreSlim> _locks
            = new ConcurrentDictionary<string, SemaphoreSlim>(StringComparer.OrdinalIgnoreCase);

        private readonly ConcurrentDictionary<string, long> _lockEpochs
            = new ConcurrentDictionary<string, long>(StringComparer.OrdinalIgnoreCase);

        private static readonly Func<string, SemaphoreSlim> _cachedAddFactory
            = _ => new SemaphoreSlim(1, 1);

        public DistributedLockStore(IExpirationQueue expirationQueue) : this(NoOpDistributedLockStoreCleanupStrategy.Instance, expirationQueue, SystemTime.Instance) { }

        public DistributedLockStore(IDistributedLockStoreCleanupStrategy cleanupStrategy, IExpirationQueue expirationQueue) : this(cleanupStrategy, expirationQueue, SystemTime.Instance) { }

        internal DistributedLockStore(IDistributedLockStoreCleanupStrategy cleanupStrategy, IExpirationQueue expirationQueue, ITime time)
        {
            _cleanupStrategy = cleanupStrategy;
            _expirationQueue = expirationQueue;
            _time = time;
        }

        public async ValueTask<Epoch> AcquireAsync(string key, long expiration, CancellationToken cancellationToken = default)
        {
            await _cleanupStrategy.CleanupAsync(this, cancellationToken).ConfigureAwait(false);

            var semaphore = _locks.GetOrAdd(key, _cachedAddFactory);
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await _expirationQueue.EnqueueAsync(key, expiration, () => semaphore.Release(), cancellationToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                // we're cancelled while waiting of adding to expiration queue, so we need Release lock by myself
                semaphore.Release();
                throw;
            }
            var result = NextEpoch();
            SaveEpoch(key, result);
            return result;
        }

        public async ValueTask<Epoch> ReleaseAsync(string key, long index, CancellationToken cancellationToken = default)
        {
            if (!_locks.TryGetValue(key, out var semaphore))
                ThrowHelper.KeyNotFoundException($"Resource with name '{key}' isn't locked");

            if (!_lockEpochs.TryGetValue(key, out var savedIndex))
                ThrowHelper.KeyNotFoundException($"Resource with name '{key}' isn't locked");

            if (savedIndex == index)
            {
                await _expirationQueue.DequeueAsync(key, cancellationToken).ConfigureAwait(false);
                semaphore!.Release();
                return NextEpoch();
            }
            return CurrentEpoch();
        }

        public async ValueTask<Epoch> SetExpirationAsync(string key, long index, long expiration, CancellationToken cancellationToken = default)
        {
            if (!_lockEpochs.TryGetValue(key, out var savedIndex))
                ThrowHelper.KeyNotFoundException($"Resource with name '{key}' isn't locked");

            if (savedIndex == index)
            {
                await _expirationQueue.UpdateExpirationAsync(key, expiration, cancellationToken).ConfigureAwait(false);
                return NextEpoch();
            }
            return CurrentEpoch();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    CallDispose(_expirationQueue);
                    CallDispose(_cleanupStrategy);
                }

                _isDisposed = true;
            }
            static void CallDispose(object obj)
            {
                if (obj is IDisposable disposable)
                    disposable.Dispose();
                else if (obj is IAsyncDisposable asyncDisposable)
                    asyncDisposable.DisposeAsync();
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        // for testing
        protected virtual internal void SaveEpoch(string key, Epoch result)
            => _lockEpochs[key] = result.Index;

        protected virtual internal Epoch NextEpoch()
            => new Epoch(Interlocked.Increment(ref _epoch), _time.UtcNow.UtcTicks);

        protected virtual internal Epoch CurrentEpoch()
            => new Epoch(Volatile.Read(ref _epoch), _time.UtcNow.UtcTicks);
    }
}
