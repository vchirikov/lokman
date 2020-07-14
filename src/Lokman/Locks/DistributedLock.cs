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
    public interface IDistributedLock : IAsyncDisposable
    {
        ValueTask<OperationResult<DistributedLockHandle>> TryAcquireAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns lock handle <paramref name="lockHandle"/> to the pool
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        ValueTask ReturnAsync(DistributedLockHandle lockHandle);
    }

    public class DistributedLock : IDistributedLock
    {
        private readonly IDistributedLockStore _store;
        /// <summary>
        /// Ordered list of requested resources
        /// </summary>
        internal StringValues _resources;

        /// <summary>
        /// Requested lease time
        /// </summary>
        internal TimeSpan _duration;

        private readonly IDistributedLockManager _manager;
        private readonly ObjectPool<DistributedLockHandle> _handlePool;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="distributedLockManager">parent object</param>
        /// <param name="poolProvider"> <see cref="LeakTrackingObjectPoolProvider"/> for example or <see cref="DefaultObjectPoolProvider"/> </param>
        /// <param name="store">the network transport for example grpc or in-memory implementation</param>
        public DistributedLock(IDistributedLockManager distributedLockManager, ObjectPoolProvider poolProvider, IDistributedLockStore store)
        {
            _manager = distributedLockManager;
            _handlePool = poolProvider.Create(new DistributedLockHandlePooledObjectPolicy(this));
            _store = store;
        }

        /// <summary>
        /// Return object to the pool in to <see cref="IDistributedLockManager"/>
        /// </summary>
        public ValueTask DisposeAsync() => _manager.ReturnAsync(this);

        /// <inheritdoc />
        public ValueTask ReturnAsync(DistributedLockHandle lockHandle)
        {
            _handlePool.Return(lockHandle);
            return default;
        }

        /// <inheritdoc />
        public ValueTask<OperationResult<DistributedLockHandle>> TryAcquireAsync(CancellationToken cancellationToken = default)
        {
            var obj = _handlePool.Get();
            throw new NotImplementedException();
        }

        /// <summary>
        /// Used when the object returns to the pool
        /// </summary>
        internal void Clear() => (_resources, _duration) = (default, default);
    }

    /// <summary>
    /// <see href="https://github.com/dotnet/aspnetcore/tree/master/src/ObjectPool/src"/>
    /// <see cref="DistributedLockHandle"/>
    /// </summary>
    internal class DistributedLockHandlePooledObjectPolicy : PooledObjectPolicy<DistributedLockHandle>
    {
        private readonly DistributedLock _lock;

        public DistributedLockHandlePooledObjectPolicy(DistributedLock lockObj) => _lock = lockObj;

        public override DistributedLockHandle Create()
        {
            return new DistributedLockHandle(_lock);
        }

        public override bool Return(DistributedLockHandle obj)
        {
            obj.Clear();
            return true;
        }
    }
}
