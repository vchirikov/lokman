using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Lokman
{
    public interface IDistributedLockStore
    {
        ValueTask<long> AcquireAsync(string key, TimeSpan duration, CancellationToken cancellationToken = default);
        ValueTask<long> UpdateAsync(string key, long token, TimeSpan duration, CancellationToken cancellationToken = default);
        ValueTask<long> ReleaseAsync(string key, long token, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns a snapshot of current store state
        /// </summary>
        ValueTask<IReadOnlyCollection<LockInfo>> GetCurrentLocksAsync(CancellationToken cancellationToken = default);
    }

    public class DistributedLockStore : IDistributedLockStore
    {
        public class LockStoreRecord
        {
            public SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);
            public long Token = -1;
            /// <summary>
            /// Used only for information, the real expiration is stored in <see cref="_expirationQueue"/>
            /// </summary>
            public DateTime ExpirationUtc;
        }

        /// <summary>
        /// Increasing store state counter
        /// </summary>
        private long _currentToken;
        private readonly ITime _time;
        private readonly IDistributedLockStoreCleanupStrategy _cleanupStrategy;
        private readonly IExpirationQueue _expirationQueue;

        internal readonly ConcurrentDictionary<string, LockStoreRecord> _locks
            = new ConcurrentDictionary<string, LockStoreRecord>(StringComparer.OrdinalIgnoreCase);

        private static readonly Func<string, LockStoreRecord> _cachedAddFactory = _ => new LockStoreRecord();

        public DistributedLockStore(IExpirationQueue expirationQueue) : this(NoOpDistributedLockStoreCleanupStrategy.Instance, expirationQueue, SystemTime.Instance) { }

        public DistributedLockStore(IDistributedLockStoreCleanupStrategy cleanupStrategy, IExpirationQueue expirationQueue) : this(cleanupStrategy, expirationQueue, SystemTime.Instance) { }

        internal DistributedLockStore(IDistributedLockStoreCleanupStrategy cleanupStrategy, IExpirationQueue expirationQueue, ITime time)
        {
            _cleanupStrategy = cleanupStrategy;
            _expirationQueue = expirationQueue;
            _time = time;
        }

        /// <inheritdoc />
        public async ValueTask<long> AcquireAsync(string key, TimeSpan duration, CancellationToken cancellationToken = default)
        {
            await _cleanupStrategy.CleanupAsync(this, cancellationToken).ConfigureAwait(false);

            var record = _locks.GetOrAdd(key, _cachedAddFactory);
            await record.Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var expiration = _time.UtcNow.AddTicks(duration.Ticks).UtcDateTime;
                await _expirationQueue.EnqueueAsync(key, expiration.Ticks, () => record.Semaphore.Release(), cancellationToken).ConfigureAwait(false);
                record.ExpirationUtc = expiration;
            }
            catch (OperationCanceledException)
            {
                // we're cancelled while waiting of adding to expiration queue, so we need Release lock by myself
                record.Semaphore.Release();
                throw;
            }
            return SaveToken(record, NextToken());
        }

        /// <inheritdoc />
        public async ValueTask<long> ReleaseAsync(string key, long token, CancellationToken cancellationToken = default)
        {
            if (!_locks.TryGetValue(key, out var record))
                ThrowHelper.KeyNotFoundException($"Resource with name '{key}' isn't locked");

            if (record!.Token == token)
            {
                await _expirationQueue.DequeueAsync(key, cancellationToken).ConfigureAwait(false);
                record.Semaphore.Release();
                return NextToken();
            }
            return CurrentToken();
        }

        /// <inheritdoc />
        public async ValueTask<long> UpdateAsync(string key, long token, TimeSpan duration, CancellationToken cancellationToken = default)
        {
            if (!_locks.TryGetValue(key, out var record))
                ThrowHelper.KeyNotFoundException($"Resource with name '{key}' isn't locked");

            if (record!.Token == token)
            {
                var expiration = _time.UtcNow.AddTicks(duration.Ticks).UtcDateTime;
                await _expirationQueue.UpdateExpirationAsync(key, expiration.Ticks, cancellationToken).ConfigureAwait(false);
                record.ExpirationUtc = expiration;
                return NextToken();
            }
            return CurrentToken();
        }

        /// <inheritdoc />
        public ValueTask<IReadOnlyCollection<LockInfo>> GetCurrentLocksAsync(CancellationToken cancellationToken = default)
            => new ValueTask<IReadOnlyCollection<LockInfo>>(_locks
                .ToArray()
                .Select(x => new LockInfo(x.Key, x.Value.Semaphore.CurrentCount <= 0, x.Value.Token, x.Value.ExpirationUtc))
                .ToList());

        // for testing
        protected virtual internal long SaveToken(LockStoreRecord pair, long token) => pair.Token = token;
        protected virtual internal long NextToken() => Interlocked.Increment(ref _currentToken);
        protected virtual internal long CurrentToken() => Volatile.Read(ref _currentToken);
    }
}
