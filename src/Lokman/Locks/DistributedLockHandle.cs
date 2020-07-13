using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;

namespace Lokman
{
    public class DistributedLockHandle : IAsyncDisposable
    {
        public Guid Id => _id;
        public StringValues Resources => _resources;

        internal StringValues _resources;
        internal Guid _id;

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

        internal void Clear() => (_resources, _id) = (default, default);
    }
}
