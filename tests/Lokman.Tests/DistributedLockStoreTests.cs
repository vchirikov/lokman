using System;
using System.Threading;
using System.Linq;
using Moq;
using Xunit;
using System.Threading.Tasks;
using System.Collections.Generic;

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

            queue.Verify(q => q.EnqueueAsync(It.IsAny<string>(), It.Is<long>(x => x == 1337), It.IsAny<Action>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task AcquireAsync_Should_CallNextEpochAndSaveEpoch()
        {
            var time = Mock.Of<ITime>(t => t.UtcNow == DateTimeOffset.Now);
            var queue = Mock.Of<IExpirationQueue>();

            var store = new Mock<DistributedLockStore>(queue, time) {
                CallBase = true,
            };

            await store.Object.AcquireAsync("foo", 1337, default).ConfigureAwait(false);

            store.Verify(x => x.NextEpoch(), Times.Once);
            store.Verify(x => x.SaveEpoch(It.Is<string>(x => x == "foo"), It.IsAny<Epoch>()), Times.Once);
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

        [Fact]
        public async Task ReleaseAsync_Should_CallNextEpoch_If_IndexEqualsSavedIndex()
        {
            var moment = new DateTimeOffset(2000, 1, 1, 0, 0, 0, default);
            var time = Mock.Of<ITime>(t => t.UtcNow == moment);
            var queue = Mock.Of<IExpirationQueue>();

            var store = new Mock<DistributedLockStore>(queue, time) {
                CallBase = true,
            };
            var savedEpoch = new Epoch(1337, moment.UtcTicks);
            store.Object._locks["foo"] = new SemaphoreSlim(0, 1);
            store.Object.SaveEpoch("foo", savedEpoch);

            await store.Object.ReleaseAsync("foo", 1337, default).ConfigureAwait(false);

            store.Verify(x => x.NextEpoch(), Times.Once);
            store.Verify(x => x.CurrentEpoch(), Times.Never);
        }

        [Fact]
        public async Task ReleaseAsync_Should_DequeueAction_If_IndexEqualsSavedIndex()
        {
            var moment = new DateTimeOffset(2000, 1, 1, 0, 0, 0, default);
            var time = Mock.Of<ITime>(t => t.UtcNow == moment);
            var queue = new Mock<IExpirationQueue>();

            var store = new Mock<DistributedLockStore>(queue.Object, time) {
                CallBase = true,
            };
            var savedEpoch = new Epoch(1337, moment.UtcTicks);
            store.Object._locks["foo"] = new SemaphoreSlim(0, 1);
            store.Object.SaveEpoch("foo", savedEpoch);

            await store.Object.ReleaseAsync("foo", 1337, default).ConfigureAwait(false);

            queue.Verify(q => q.DequeueAsync(It.Is<string>(x => x == "foo"), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ReleaseAsync_Should_ReturnCurrentEpoch_If_IndexNotEqualsSavedIndex()
        {
            var moment = new DateTimeOffset(2000, 1, 1, 0, 0, 0, default);
            var time = Mock.Of<ITime>(t => t.UtcNow == moment);
            var queue = Mock.Of<IExpirationQueue>();

            var store = new Mock<DistributedLockStore>(queue, time) {
                CallBase = true,
            };
            var savedEpoch = new Epoch(1337, moment.UtcTicks);
            store.Object._locks["foo"] = new SemaphoreSlim(0, 1);
            store.Object.SaveEpoch("foo", savedEpoch);

            await store.Object.ReleaseAsync("foo", 31337, default).ConfigureAwait(false);

            store.Verify(x => x.NextEpoch(), Times.Never);
            store.Verify(x => x.CurrentEpoch(), Times.Once);
        }

        [Fact]
        public async Task ReleaseAsync_Should_ThrowKeyNotFoundException_If_LockIsNotFound()
        {
            var store = new DistributedLockStore(Mock.Of<IExpirationQueue>(), Mock.Of<ITime>());
            await Assert.ThrowsAsync<KeyNotFoundException>(async () => {
                await store.ReleaseAsync("foo", 0, default).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        [Fact]
        public async Task SetExpirationAsync_Should_CallNextEpoch_If_IndexEqualsSavedIndex()
        {
            var moment = new DateTimeOffset(2000, 1, 1, 0, 0, 0, default);
            var time = Mock.Of<ITime>(t => t.UtcNow == moment);
            var queue = Mock.Of<IExpirationQueue>();

            var store = new Mock<DistributedLockStore>(queue, time) {
                CallBase = true,
            };
            var savedEpoch = new Epoch(1337, moment.UtcTicks);
            store.Object._locks["foo"] = new SemaphoreSlim(0, 1);
            store.Object.SaveEpoch("foo", savedEpoch);

            await store.Object.SetExpirationAsync("foo", 1337, 31337, default).ConfigureAwait(false);

            store.Verify(x => x.NextEpoch(), Times.Once);
            store.Verify(x => x.CurrentEpoch(), Times.Never);
        }

        [Fact]
        public async Task SetExpirationAsync_Should_UpdateExpirationAsync_If_IndexEqualsSavedIndex()
        {
            var moment = new DateTimeOffset(2000, 1, 1, 0, 0, 0, default);
            var time = Mock.Of<ITime>(t => t.UtcNow == moment);
            var queue = new Mock<IExpirationQueue>();

            var store = new Mock<DistributedLockStore>(queue.Object, time) {
                CallBase = true,
            };
            var savedEpoch = new Epoch(1337, moment.UtcTicks);
            store.Object._locks["foo"] = new SemaphoreSlim(0, 1);
            store.Object.SaveEpoch("foo", savedEpoch);

            await store.Object.SetExpirationAsync("foo", 1337, 31337, default).ConfigureAwait(false);

            queue.Verify(q => q.UpdateExpirationAsync(
                It.Is<string>(x => x == "foo"),
                It.Is<long>(x => x == 31337),
                It.IsAny<CancellationToken>())
            , Times.Once);

        }

        [Fact]
        public async Task SetExpirationAsync_Should_ReturnCurrentEpoch_If_IndexNotEqualsSavedIndex()
        {
            var moment = new DateTimeOffset(2000, 1, 1, 0, 0, 0, default);
            var time = Mock.Of<ITime>(t => t.UtcNow == moment);
            var queue = Mock.Of<IExpirationQueue>();

            var store = new Mock<DistributedLockStore>(queue, time) {
                CallBase = true,
            };
            var savedEpoch = new Epoch(1337, moment.UtcTicks);
            store.Object._locks["foo"] = new SemaphoreSlim(0, 1);
            store.Object.SaveEpoch("foo", savedEpoch);

            await store.Object.SetExpirationAsync("foo", 31337, 31337, default).ConfigureAwait(false);

            store.Verify(x => x.NextEpoch(), Times.Never);
            store.Verify(x => x.CurrentEpoch(), Times.Once);
        }

        [Fact]
        public async Task SetExpirationAsync_Should_ThrowKeyNotFoundException_If_LockIsNotFound()
        {
            var store = new DistributedLockStore(Mock.Of<IExpirationQueue>(), Mock.Of<ITime>());
            await Assert.ThrowsAsync<KeyNotFoundException>(async () => {
                await store.SetExpirationAsync("foo", 0, 0, default).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }
    }
}
