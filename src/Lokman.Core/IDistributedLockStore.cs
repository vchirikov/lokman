using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lokman
{
    public interface IDistributedLockStore
    {
        ValueTask<long> AcquireAsync(string key, TimeSpan duration, CancellationToken cancellationToken = default);
        ValueTask<long> UpdateAsync(string key, long token, TimeSpan duration, CancellationToken cancellationToken = default);
        ValueTask<long> ReleaseAsync(string key, long token, CancellationToken cancellationToken = default);
    }

    public class DistributedLockStore : IDistributedLockStore, IAsyncDisposable, IDisposable
    {
        /// <summary>
        /// Increasing store state counter
        /// </summary>
        private long _currentTocken;
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

        public async ValueTask<long> AcquireAsync(string key, TimeSpan duration, CancellationToken cancellationToken = default)
        {
            await _cleanupStrategy.CleanupAsync(this, cancellationToken).ConfigureAwait(false);

            var semaphore = _locks.GetOrAdd(key, _cachedAddFactory);
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                long expiration = unchecked(_time.UtcNow.Ticks + duration.Ticks);
                await _expirationQueue.EnqueueAsync(key, expiration, () => semaphore.Release(), cancellationToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                // we're cancelled while waiting of adding to expiration queue, so we need Release lock by myself
                semaphore.Release();
                throw;
            }
            var result = NextToken();
            SaveToken(key, result);
            return result;
        }

        public async ValueTask<long> ReleaseAsync(string key, long token, CancellationToken cancellationToken = default)
        {
            if (!_locks.TryGetValue(key, out var semaphore))
                ThrowHelper.KeyNotFoundException($"Resource with name '{key}' isn't locked");

            if (!_lockEpochs.TryGetValue(key, out var savedIndex))
                ThrowHelper.KeyNotFoundException($"Resource with name '{key}' isn't locked");

            if (savedIndex == token)
            {
                await _expirationQueue.DequeueAsync(key, cancellationToken).ConfigureAwait(false);
                semaphore!.Release();
                return NextToken();
            }
            return CurrentToken();
        }

        public async ValueTask<long> UpdateAsync(string key, long token, TimeSpan duration, CancellationToken cancellationToken = default)
        {
            if (!_lockEpochs.TryGetValue(key, out var savedIndex))
                ThrowHelper.KeyNotFoundException($"Resource with name '{key}' isn't locked");

            if (savedIndex == token)
            {
                long expiration = unchecked(_time.UtcNow.Ticks + duration.Ticks);
                await _expirationQueue.UpdateExpirationAsync(key, expiration, cancellationToken).ConfigureAwait(false);
                return NextToken();
            }
            return CurrentToken();
        }

        protected virtual async ValueTask DisposeAsyncCore()
        {
            if (_cleanupStrategy is IAsyncDisposable asyncDisposable1)
                await asyncDisposable1.DisposeAsync().ConfigureAwait(false);

            if (_expirationQueue is IAsyncDisposable asyncDisposable2)
                await asyncDisposable2.DisposeAsync().ConfigureAwait(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    (_cleanupStrategy as IDisposable)?.Dispose();
                    (_expirationQueue as IDisposable)?.Dispose();
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

        // for testing
        protected virtual internal void SaveToken(string key, long token)
            => _lockEpochs[key] = token;

        protected virtual internal long NextToken()
            => Interlocked.Increment(ref _currentTocken);

        protected virtual internal long CurrentToken()
            => Volatile.Read(ref _currentTocken);
    }
}
