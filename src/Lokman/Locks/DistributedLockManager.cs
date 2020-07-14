using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Primitives;

namespace Lokman
{
    public class DistributedLockManagerConfig
    {
        public TimeSpan DefaultDuration { get; set; } = TimeSpan.FromSeconds(0);
    }

    /// <summary>
    /// Factory that creates a <see cref="IDistributedLock"/> associated with some resources
    /// </summary>
    public interface IDistributedLockManager : IAsyncDisposable
    {
        /// <summary>
        /// Creates a lock <see cref="IDistributedLock"/> which can try to lock <paramref name="resources"/>
        /// for <paramref name="duration"/>
        /// </summary>
        /// <param name="resources">The list of requested resources</param>
        /// <param name="duration">The requested lease time</param>
        /// <returns><see cref="IDistributedLock"/> associated <paramref name="resources"/> some resources</returns>
        IDistributedLock Create(StringValues resources, TimeSpan? duration = null);

        /// <summary>
        /// Returns lock object <paramref name="distributedLock"/> to the pool
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        ValueTask ReturnAsync(IDistributedLock distributedLock);
    }

    /// <summary>
    /// Connection pool abstraction
    /// </summary>
    public class DistributedLockManager : IDistributedLockManager
    {
        private readonly DistributedLockManagerConfig _config;
        private readonly IDistributedLockStore _transport;
        private readonly ObjectPool<DistributedLock> _lockPool;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="config"></param>
        /// <param name="poolProvider"> <see cref="LeakTrackingObjectPoolProvider"/> for example or <see cref="DefaultObjectPoolProvider"/> </param>
        /// <param name="transport">the network transport for example grpc or in-memory implementation</param>
        public DistributedLockManager(DistributedLockManagerConfig config, ObjectPoolProvider poolProvider, IDistributedLockStore transport)
        {
            _config = config;
            _transport = transport;
            _lockPool = poolProvider.Create(new DistributedLockPooledObjectPolicy(this, poolProvider, _transport));
        }

        /// <inheritdoc />
        public IDistributedLock Create(StringValues resources, TimeSpan? duration = null)
        {
            var lockObj = _lockPool.Get();
            (lockObj._resources, lockObj._duration) = (resources, duration ?? _config.DefaultDuration);
            return lockObj;
        }

        /// <inheritdoc />
        public ValueTask ReturnAsync(IDistributedLock distributedLock)
        {
            _lockPool.Return((DistributedLock)distributedLock);
            return default;
        }

        public ValueTask DisposeAsync() => default;
    }

    /// <summary>
    /// <see href="https://github.com/dotnet/aspnetcore/tree/master/src/ObjectPool/src"/>
    /// <see cref="DistributedLock"/>
    /// </summary>
    internal class DistributedLockPooledObjectPolicy : PooledObjectPolicy<DistributedLock>
    {
        private readonly DistributedLockManager _manager;
        private readonly ObjectPoolProvider _poolProvider;
        private readonly IDistributedLockStore _store;

        public DistributedLockPooledObjectPolicy(DistributedLockManager manager, ObjectPoolProvider poolProvider, IDistributedLockStore transport)
        {
            _manager = manager;
            _poolProvider = poolProvider;
            _store = transport;
        }

        public override DistributedLock Create()
            => new DistributedLock(_manager, _poolProvider, _store);

        public override bool Return(DistributedLock obj)
        {
            obj.Clear();
            return true;
        }
    }
}
