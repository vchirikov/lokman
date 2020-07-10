using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Lokman
{
    public interface IDistributedLockStore
    {
        ValueTask<Epoch> AcquireAsync(string key, long expiration, CancellationToken cancellationToken = default);
        ValueTask<Epoch> SetExpirationAsync(string key, long index, long expiration, CancellationToken cancellationToken = default);
        ValueTask<Epoch> ReleaseAsync(string key, long index, CancellationToken cancellationToken = default);
        ValueTask CollectGarbageAsync(CancellationToken cancellationToken = default);
    }

    public class DistributedLockStore : IDistributedLockStore
    {
        /// <summary>
        /// Increasing store state counter
        /// </summary>
        private long _epoch;

        private readonly ITime _time;

        private readonly IExpirationQueue _expirationQueue;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks
            = new ConcurrentDictionary<string, SemaphoreSlim>(StringComparer.OrdinalIgnoreCase);

        private static readonly Func<string, SemaphoreSlim> _cachedAddFactory
            = _ => new SemaphoreSlim(1,1) ;

        public DistributedLockStore(IExpirationQueue expirationQueue) : this(expirationQueue, SystemTime.Instance) { }

        internal DistributedLockStore(IExpirationQueue expirationQueue, ITime time)
        {
            _expirationQueue = expirationQueue;
            _time = time;
        }

        public async ValueTask<Epoch> AcquireAsync(string key, long expiration, CancellationToken cancellationToken = default)
        {
            var semaphore = _locks.GetOrAdd(key, _cachedAddFactory);
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await _expirationQueue.EqueueAsync(expiration, () => semaphore.Release(), cancellationToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                // we're cancelled while waiting of adding to expiration queue, so we need Release lock by myself
                semaphore.Release();
                throw;
            }
            return NextEpoch();
        }

        public ValueTask CollectGarbageAsync(CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public ValueTask<Epoch> ReleaseAsync(string key, long index, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public ValueTask<Epoch> SetExpirationAsync(string key, long index, long expiration, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }


        protected virtual internal Epoch NextEpoch()
            => new Epoch(Interlocked.Increment(ref _epoch), _time.UtcNow.UtcTicks);

        protected virtual internal Epoch CurrentEpoch()
            => new Epoch(Volatile.Read(ref _epoch), _time.UtcNow.UtcTicks);
    }
}
