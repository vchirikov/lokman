using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Lokman
{
    public sealed class DistributedLockHandle : IAsyncDisposable
    {
        /// <summary>
        /// The information about locked resources.
        /// </summary>
        public IReadOnlyList<LockHandleRecord> Records => _records;

        internal readonly List<LockHandleRecord> _records = new List<LockHandleRecord>();

        private readonly IDistributedLock _lock;

        public DistributedLockHandle(IDistributedLock lockObj) => _lock = lockObj;

        /// <summary>
        /// Releases lock and returns the object to the pool in to <see cref="IDistributedLock"/>
        /// The operation is not thread-safe
        /// </summary>
        public async ValueTask<OperationResult<DistributedLockHandle, Error>> UpdateAsync(TimeSpan duration, CancellationToken cancellationToken = default)
        {
            var oldRecords = _records.ToArray();
            Clear();
            foreach (var (key, token) in oldRecords)
            {
                var result = await _lock.UpdateAsync(key, token, duration, cancellationToken).ConfigureAwait(false);
                if (result.IsError)
                {
                    // ToDo: should we throw here? maybe add option about it in config, because we can create deadlock here (if partial release keys)
                }
                else
                {
                    _records.Add((key, result.Value));
                }
            }
            return this;
        }

        /// <summary>
        /// Releases lock and returns the object to the pool in to <see cref="IDistributedLock"/>
        /// The operation is not thread-safe
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            _records.Reverse();
            foreach (var (key, token) in _records)
            {
                var result = await _lock.ReleaseAsync(key, token, default).ConfigureAwait(false);
                if (result.IsError)
                {
                    // ToDo: should we throw here? maybe add option about it in config, because we can create deadlock here (if partial release keys)
                }
            }
            await _lock.ReturnAsync(this).ConfigureAwait(false);
        }

        internal void Clear()
        {
            _records.Clear();
        }
    }
}
