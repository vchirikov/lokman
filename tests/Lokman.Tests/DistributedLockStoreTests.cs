using System;
using System.Threading;
using Moq;
using Xunit;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Lokman.Tests
{
    public class DistributedLockStoreTests
    {
        [Fact]
        public async Task AcquireAsync_Should_CallCleanupAsync()
        {
            var time = Mock.Of<ITime>(t => t.UtcNow == DateTimeOffset.Now);
            var cleanupStrategy = new Mock<IDistributedLockStoreCleanupStrategy>();
            using var store = new DistributedLockStore(cleanupStrategy.Object, Mock.Of<IExpirationQueue>(), time);

            await store.AcquireAsync("foo", 1337, default).ConfigureAwait(false);

            cleanupStrategy.Verify(c => c.CleanupAsync(It.IsAny<IDistributedLockStore>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task AcquireAsync_Should_EqueueAction()
        {
            var time = Mock.Of<ITime>(t => t.UtcNow == DateTimeOffset.Now);
            var queue = new Mock<IExpirationQueue>();
            using var store = new DistributedLockStore(Mock.Of<IDistributedLockStoreCleanupStrategy>(), queue.Object, time);

            await store.AcquireAsync("foo", 1337, default).ConfigureAwait(false);

            queue.Verify(q => q.EnqueueAsync(It.IsAny<string>(), It.Is<long>(x => x == 1337), It.IsAny<Action>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task AcquireAsync_Should_CallNextEpochAndSaveEpoch()
        {
            var time = Mock.Of<ITime>(t => t.UtcNow == DateTimeOffset.Now);
            var queue = Mock.Of<IExpirationQueue>();

            var store = new Mock<DistributedLockStore>(Mock.Of<IDistributedLockStoreCleanupStrategy>(), queue, time) {
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
            using var store = new DistributedLockStore(Mock.Of<IDistributedLockStoreCleanupStrategy>(), Mock.Of<IExpirationQueue>(), time);

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

            var store = new Mock<DistributedLockStore>(Mock.Of<IDistributedLockStoreCleanupStrategy>(), queue, time) {
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

            var store = new Mock<DistributedLockStore>(Mock.Of<IDistributedLockStoreCleanupStrategy>(), queue.Object, time) {
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

            var store = new Mock<DistributedLockStore>(Mock.Of<IDistributedLockStoreCleanupStrategy>(), queue, time) {
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
            using var store = new DistributedLockStore(Mock.Of<IDistributedLockStoreCleanupStrategy>(), Mock.Of<IExpirationQueue>(), Mock.Of<ITime>());
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

            var store = new Mock<DistributedLockStore>(Mock.Of<IDistributedLockStoreCleanupStrategy>(), queue, time) {
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

            var store = new Mock<DistributedLockStore>(Mock.Of<IDistributedLockStoreCleanupStrategy>(), queue.Object, time) {
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

            var store = new Mock<DistributedLockStore>(Mock.Of<IDistributedLockStoreCleanupStrategy>(), queue, time) {
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
            using var store = new DistributedLockStore(Mock.Of<IDistributedLockStoreCleanupStrategy>(), Mock.Of<IExpirationQueue>(), Mock.Of<ITime>());
            await Assert.ThrowsAsync<KeyNotFoundException>(async () => {
                await store.SetExpirationAsync("foo", 0, 0, default).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }


        [Fact]
        public void Dispose_Should_CallDisposeOnDependences()
        {
            var disposable1 = new Mock<IDisposable>();
            var disposable2 = new Mock<IDisposable>();

            using (new DistributedLockStore(
                disposable1.As<IDistributedLockStoreCleanupStrategy>().Object,
                disposable2.As<IExpirationQueue>().Object,
                Mock.Of<ITime>()
            ))
            { }

            disposable1.Verify(s => s.Dispose(), Times.Once);
            disposable2.Verify(s => s.Dispose(), Times.Once);

        }

        [Fact]
        public async Task DisposeAsync_Should_CallDisposeAsyncOnDependences()
        {
            var asyncDisposable1 = new Mock<IAsyncDisposable>();
            var asyncDisposable2 = new Mock<IAsyncDisposable>();

            await using (new DistributedLockStore(
                asyncDisposable1.As<IDistributedLockStoreCleanupStrategy>().Object,
                asyncDisposable2.As<IExpirationQueue>().Object,
                Mock.Of<ITime>()
            ).ConfigureAwait(false))
            { }

            asyncDisposable1.Verify(s => s.DisposeAsync(), Times.Once);
            asyncDisposable2.Verify(s => s.DisposeAsync(), Times.Once);
        }


    }
}
