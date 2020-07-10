using System;
using System.Threading;
using System.Linq;
using Moq;
using Xunit;
using System.Threading.Tasks;

namespace Lokman.Tests
{
    public class DistributedLockStoreTests
    {
        [Fact]
        public async Task AcquireAsync_Should_EqueueAction()
        {
            var time = Mock.Of<ITime>(t => t.UtcNow == DateTimeOffset.Now);
            var queue = new Mock<IExpirationQueue>();
            var store = new DistributedLockStore(queue.Object, time);

            await store.AcquireAsync("foo", 1337, default).ConfigureAwait(false);

            queue.Verify(q => q.EqueueAsync(It.Is<long>(x => x == 1337), It.IsAny<Action>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task AcquireAsync_Should_CallNextEpoch()
        {
            var time = Mock.Of<ITime>(t => t.UtcNow == DateTimeOffset.Now);
            var queue = Mock.Of<IExpirationQueue>();

            var store = new Mock<DistributedLockStore>(queue, time) {
                CallBase = true,
            };

            await store.Object.AcquireAsync("foo", 1337, default).ConfigureAwait(false);

            store.Verify(x => x.NextEpoch(), Times.Once);
        }

        [Fact]
        public void NextEpoch_Should_IncrementEpoch()
        {
            var time = Mock.Of<ITime>(t => t.UtcNow == new DateTimeOffset(2000, 1, 1, 0, 0, 0, default));
            var store = new DistributedLockStore(Mock.Of<IExpirationQueue>(), time);

            var before = store.CurrentEpoch();
            var result = store.NextEpoch();
            var after = store.CurrentEpoch();

            Assert.NotEqual(before, result);
            Assert.Equal(result, after);
            Assert.True(after.Index > before.Index);
        }
    }
}
