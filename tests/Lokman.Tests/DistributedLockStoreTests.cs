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
            var store = new DistributedLockStore(cleanupStrategy.Object, Mock.Of<IExpirationQueue>(), time);

            await store.AcquireAsync("foo", default, default).ConfigureAwait(false);

            cleanupStrategy.Verify(c => c.CleanupAsync(It.IsAny<IDistributedLockStore>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task AcquireAsync_Should_EqueueAction()
        {
            var moment = new DateTimeOffset(2000, 1, 1, 0, 0, 0, default);
            var time = Mock.Of<ITime>(t => t.UtcNow == moment);
            var queue = new Mock<IExpirationQueue>();
            var store = new DistributedLockStore(Mock.Of<IDistributedLockStoreCleanupStrategy>(), queue.Object, time);

            await store.AcquireAsync("foo", default, default).ConfigureAwait(false);

            queue.Verify(q => q.EnqueueAsync(It.IsAny<string>(), moment.Ticks, It.IsAny<Action>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task AcquireAsync_Should_CallNextTokenAndSaveToken()
        {
            var time = Mock.Of<ITime>(t => t.UtcNow == DateTimeOffset.Now);
            var queue = Mock.Of<IExpirationQueue>();

            var store = new Mock<DistributedLockStore>(Mock.Of<IDistributedLockStoreCleanupStrategy>(), queue, time) {
                CallBase = true,
            };

            await store.Object.AcquireAsync("foo", default, default).ConfigureAwait(false);

            store.Verify(x => x.NextToken(), Times.Once);
            store.Verify(x => x.SaveToken(It.IsAny<DistributedLockStore.LockStoreRecord>(), It.IsAny<long>()), Times.Once);
        }

        [Fact]
        public void NextToken_Should_IncrementToken()
        {
            var time = Mock.Of<ITime>(t => t.UtcNow == new DateTimeOffset(2000, 1, 1, 0, 0, 0, default));
            var store = new DistributedLockStore(Mock.Of<IDistributedLockStoreCleanupStrategy>(), Mock.Of<IExpirationQueue>(), time);

            var before = store.CurrentToken();
            var result = store.NextToken();
            var after = store.CurrentToken();

            Assert.NotEqual(before, result);
            Assert.Equal(result, after);
            Assert.True(after > before);
        }

        [Fact]
        public async Task ReleaseAsync_Should_CallNextToken_If_TokenEqualsSavedToken()
        {
            var moment = new DateTimeOffset(2000, 1, 1, 0, 0, 0, default);
            var time = Mock.Of<ITime>(t => t.UtcNow == moment);
            var queue = Mock.Of<IExpirationQueue>();

            var store = new Mock<DistributedLockStore>(Mock.Of<IDistributedLockStoreCleanupStrategy>(), queue, time) {
                CallBase = true,
            };
            var savedToken = 1337;
            var record = new DistributedLockStore.LockStoreRecord();
            record.Semaphore.Wait(1000);
            store.Object._locks["foo"] = record;
            store.Object.SaveToken(record, savedToken);

            await store.Object.ReleaseAsync("foo", 1337, default).ConfigureAwait(false);

            store.Verify(x => x.NextToken(), Times.Once);
            store.Verify(x => x.CurrentToken(), Times.Never);
        }

        [Fact]
        public async Task ReleaseAsync_Should_DequeueAction_If_TokenEqualsSavedToken()
        {
            var moment = new DateTimeOffset(2000, 1, 1, 0, 0, 0, default);
            var time = Mock.Of<ITime>(t => t.UtcNow == moment);
            var queue = new Mock<IExpirationQueue>();

            var store = new Mock<DistributedLockStore>(Mock.Of<IDistributedLockStoreCleanupStrategy>(), queue.Object, time) {
                CallBase = true,
            };
            var savedToken = 1337;
            var record = new DistributedLockStore.LockStoreRecord();
            record.Semaphore.Wait(1000);
            store.Object._locks["foo"] = record;
            store.Object.SaveToken(record, savedToken);

            await store.Object.ReleaseAsync("foo", 1337, default).ConfigureAwait(false);

            queue.Verify(q => q.DequeueAsync("foo", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ReleaseAsync_Should_ReturnCurrentToken_If_TokenNotEqualsSavedToken()
        {
            var moment = new DateTimeOffset(2000, 1, 1, 0, 0, 0, default);
            var time = Mock.Of<ITime>(t => t.UtcNow == moment);
            var queue = Mock.Of<IExpirationQueue>();

            var store = new Mock<DistributedLockStore>(Mock.Of<IDistributedLockStoreCleanupStrategy>(), queue, time) {
                CallBase = true,
            };
            var savedToken = 1337;
            var record = new DistributedLockStore.LockStoreRecord();
            record.Semaphore.Wait(1000);
            store.Object._locks["foo"] = record;
            store.Object.SaveToken(record, savedToken);

            await store.Object.ReleaseAsync("foo", 31337, default).ConfigureAwait(false);

            store.Verify(x => x.NextToken(), Times.Never);
            store.Verify(x => x.CurrentToken(), Times.Once);
        }

        [Fact]
        public async Task ReleaseAsync_Should_ThrowKeyNotFoundException_If_LockIsNotFound()
        {
            var store = new DistributedLockStore(Mock.Of<IDistributedLockStoreCleanupStrategy>(), Mock.Of<IExpirationQueue>(), Mock.Of<ITime>());
            await Assert.ThrowsAsync<KeyNotFoundException>(async () => {
                await store.ReleaseAsync("foo", 0, default).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        [Fact]
        public async Task UpdateAsync_Should_CallNextToken_If_IndexEqualsSavedToken()
        {
            var moment = new DateTimeOffset(2000, 1, 1, 0, 0, 0, default);
            var time = Mock.Of<ITime>(t => t.UtcNow == moment);
            var queue = Mock.Of<IExpirationQueue>();

            var store = new Mock<DistributedLockStore>(Mock.Of<IDistributedLockStoreCleanupStrategy>(), queue, time) {
                CallBase = true,
            };
            var savedToken = 1337;
            var record = new DistributedLockStore.LockStoreRecord();
            record.Semaphore.Wait(1000);
            store.Object._locks["foo"] = record;
            store.Object.SaveToken(record, savedToken);

            await store.Object.UpdateAsync("foo", 1337, TimeSpan.FromTicks(31337), default).ConfigureAwait(false);

            store.Verify(x => x.NextToken(), Times.Once);
            store.Verify(x => x.CurrentToken(), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_Should_UpdateExpirationAsync_If_IndexEqualsSavedToken()
        {
            var moment = new DateTimeOffset(2000, 1, 1, 0, 0, 0, default);
            var time = Mock.Of<ITime>(t => t.UtcNow == moment);
            var queue = new Mock<IExpirationQueue>();

            var store = new Mock<DistributedLockStore>(Mock.Of<IDistributedLockStoreCleanupStrategy>(), queue.Object, time) {
                CallBase = true,
            };
            var savedToken = 1337;
            var record = new DistributedLockStore.LockStoreRecord();
            record.Semaphore.Wait(1000);
            store.Object._locks["foo"] = record;
            store.Object.SaveToken(record, savedToken);

            await store.Object.UpdateAsync("foo", 1337, TimeSpan.FromTicks(31337), default).ConfigureAwait(false);

            queue.Verify(q => q.UpdateExpirationAsync(
                "foo",
                31337 + moment.Ticks,
                It.IsAny<CancellationToken>())
            , Times.Once);

        }

        [Fact]
        public async Task UpdateAsync_Should_ReturnCurrentToken_If_IndexNotEqualsSavedToken()
        {
            var moment = new DateTimeOffset(2000, 1, 1, 0, 0, 0, default);
            var time = Mock.Of<ITime>(t => t.UtcNow == moment);
            var queue = Mock.Of<IExpirationQueue>();

            var store = new Mock<DistributedLockStore>(Mock.Of<IDistributedLockStoreCleanupStrategy>(), queue, time) {
                CallBase = true,
            };
            var savedToken = 1337;
            var record = new DistributedLockStore.LockStoreRecord();
            record.Semaphore.Wait(1000);
            store.Object._locks["foo"] = record;
            store.Object.SaveToken(record, savedToken);

            await store.Object.UpdateAsync("foo", 31337, TimeSpan.FromTicks(31337), default).ConfigureAwait(false);

            store.Verify(x => x.NextToken(), Times.Never);
            store.Verify(x => x.CurrentToken(), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_Should_ThrowKeyNotFoundException_If_LockIsNotFound()
        {
            var store = new DistributedLockStore(Mock.Of<IDistributedLockStoreCleanupStrategy>(), Mock.Of<IExpirationQueue>(), Mock.Of<ITime>());
            await Assert.ThrowsAsync<KeyNotFoundException>(async () => {
                await store.UpdateAsync("foo", 0, default, default).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }
    }
}
