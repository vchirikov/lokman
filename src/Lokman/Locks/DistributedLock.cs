using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Primitives;
using static Lokman.OperationResultHelpers;

namespace Lokman
{
    public interface IDistributedLock : IAsyncDisposable
    {
        ValueTask<OperationResult<DistributedLockHandle, Error>> AcquireAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// You sholdn't use this in most cases, use <see cref="DistributedLockHandle.DisposeAsync"/> for releasing the taken lock
        /// </summary>
        ValueTask<OperationResult<long, Error>> ReleaseAsync(string key, long token, CancellationToken cancellationToken = default);

        /// <summary>
        /// You sholdn't use this in most cases, use <see cref="DistributedLockHandle.UpdateAsync"/> for updating the taken lock duration
        /// </summary>
        ValueTask<OperationResult<long, Error>> UpdateAsync(string key, long token, TimeSpan duration, CancellationToken cancellationToken = default);

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
        internal StringValues _keys;

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
        public async ValueTask<OperationResult<DistributedLockHandle, Error>> AcquireAsync(CancellationToken cancellationToken = default)
        {
            Exception? exception = null;
            var handle = _handlePool.Get();
            try
            {
                foreach (var key in _keys)
                {
                    if (key is null)
                        continue;
                    long token = 0;
                    // ToDo: add usage of Polly policies here
                    token = await _store.AcquireAsync(key, _duration, cancellationToken).ConfigureAwait(false);
                    handle._records.Add((key, token));
                }
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            finally
            {
                if (exception != null)
                {
                    handle._records.Reverse();
                    foreach (var (resource, token) in handle._records)
                    {
                        try
                        {
                            await _store.ReleaseAsync(resource, token).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            // ToDo: add logging here
                            System.Diagnostics.Debug.WriteLine($"Exception in error handling: {ex}");
                        }
                    }
                }
            }
            if (exception != null)
                return Error(exception, ErrorCodes.Client.AcquireLockError, $"Can't get lock '{_keys.ToString() ?? "null"}' for duration {_duration}. Cancellation status: {cancellationToken.IsCancellationRequested}.");

            return handle;
        }

        /// <inheritdoc />
        public async ValueTask<OperationResult<long, Error>> ReleaseAsync(string key, long token, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _store.ReleaseAsync(key, token, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return Error(ex, ErrorCodes.Client.ReleaseLockError, $"Can't release lock with key: '{key ?? "null"}'.");
            }
        }

        /// <inheritdoc />
        public async ValueTask<OperationResult<long, Error>> UpdateAsync(string key, long token, TimeSpan duration, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _store.UpdateAsync(key, token, duration, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return Error(ex, ErrorCodes.Client.UpdateLockError, $"Can't release lock with key: '{key ?? "null"}'.");
            }
        }

        /// <summary>
        /// Used when the object returns to the pool
        /// </summary>
        internal void Clear() => (_keys, _duration) = (default, default);
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
