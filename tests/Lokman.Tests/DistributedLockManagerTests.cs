using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.ObjectPool;
using Moq;
using Xunit;

namespace Lokman.Tests
{
    public class DistributedLockManagerTests
    {
        [Fact]
        public async Task DistributedLock_Should_BeReturnedToPool()
        {
            var manager = CreateManager();
            await using var _ = manager.ConfigureAwait(false);
            IDistributedLock? lock1 = default, lock2 = default;
            try
            {
                lock1 = manager.Create("foo", TimeSpan.FromSeconds(5));
                await lock1.DisposeAsync().ConfigureAwait(false);
                lock2 = manager.Create("foo", TimeSpan.FromSeconds(5));
                lock2.Should().BeSameAs(lock2, "The manager should return same object from the pool after DisposeAsync");
            }
            finally
            {
                if (lock1 != null)
                    await lock1.DisposeAsync().ConfigureAwait(false);
                if (lock2 != null)
                    await lock2.DisposeAsync().ConfigureAwait(false);
            }
        }

        private static DistributedLockManager CreateManager() => new DistributedLockManager(new DistributedLockManagerConfig(),
                        new LeakTrackingObjectPoolProvider(new DefaultObjectPoolProvider()), Mock.Of<IDistributedLockStore>());
    }
}
