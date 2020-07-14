using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lokman
{
    public class DistributedLockHandle : IAsyncDisposable
    {
        public Guid Id => _id;
        public IEnumerable<(string Resource, long Token)> Resources => _resources;

        internal Guid _id;
        internal readonly List<(string Resource, long Token)> _resources = new List<(string Resource, long Token)>();

        private readonly IDistributedLock _lock;

        public DistributedLockHandle(IDistributedLock lockObj)
            => _lock = lockObj;

        /// <summary>
        /// Release lock and Return object to the pool in to <see cref="IDistributedLock"/>
        /// </summary>
        public ValueTask DisposeAsync()
        {
            // ToDo: release lock
            return _lock.ReturnAsync(this);
        }

        internal void Clear()
        {
            _id = default;
            _resources.Clear();
        }
    }
}
