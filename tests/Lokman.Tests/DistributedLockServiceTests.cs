using Xunit;
using System.Threading.Tasks;
using Lokman.Protos;
using Google.Type;
using System;
using Moq;
using System.Threading;

namespace Lokman.Tests
{
    public class DistributedLockServiceTests
    {
        [Fact]
        public async Task ProcessAsync_Should_ParseToAcquireAsync()
        {
            var store = new Mock<IDistributedLockStore>();
            var acquireRequest = new LockRequest() {
                Expiration = 100,
                Index = -1,
                Key = "foo",
            };

            using var service = new DistributedLockService(Mock.Of<IEventLogger<DistributedLockService>>(),
                store.Object,
                Mock.Of<ITime>()
            );

            await service.ProcessAsync(acquireRequest, default).ConfigureAwait(false);

            store.Verify(s => s.AcquireAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ProcessAsync_Should_ParseToSetExpirationAsync()
        {
            var store = new Mock<IDistributedLockStore>();

            var acquireRequest = new LockRequest() {
                Expiration = 100,
                Index = 1,
                Key = "foo",
            };

            using var service = new DistributedLockService(Mock.Of<IEventLogger<DistributedLockService>>(),
                store.Object,
                Mock.Of<ITime>()
            );

            await service.ProcessAsync(acquireRequest, default).ConfigureAwait(false);

            store.Verify(s => s.SetExpirationAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ProcessAsync_Should_ParseToReleaseAsync_If_ExpirationIsZero()
        {
            var store = new Mock<IDistributedLockStore>();

            var acquireRequest = new LockRequest() {
                Expiration = 0,
                Index = 0,
                Key = "foo",
            };

            using var service = new DistributedLockService(Mock.Of<IEventLogger<DistributedLockService>>(),
                store.Object,
                Mock.Of<ITime>()
            );

            await service.ProcessAsync(acquireRequest, default).ConfigureAwait(false);

            store.Verify(s => s.ReleaseAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ProcessAsync_Should_ParseToReleaseAsync()
        {
            var moment = new DateTimeOffset(1000, TimeSpan.Zero);
            var time = Mock.Of<ITime>(t => t.UtcNow == moment);
            var store = new Mock<IDistributedLockStore>();

            var acquireRequest = new LockRequest() {
                Expiration = 100,
                Index = 1,
                Key = "foo",
            };

            using var service = new DistributedLockService(Mock.Of<IEventLogger<DistributedLockService>>(),
                store.Object,
                time
            );

            await service.ProcessAsync(acquireRequest, default).ConfigureAwait(false);

            store.Verify(s => s.ReleaseAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public void Dispose_Should_CallDisposeOnDependences()
        {
            var store = new Mock<IDistributedLockStore>();
            var disposableStore = store.As<IDisposable>();

            using (new DistributedLockService(
                Mock.Of<IEventLogger<DistributedLockService>>(),
                store.Object,
                Mock.Of<ITime>()
            ))
            { }

            disposableStore.Verify(s => s.Dispose(), Times.Once);
        }

        [Fact]
        public async Task DisposeAsync_Should_CallDisposeAsyncOnDependences()
        {
            var store = new Mock<IDistributedLockStore>();
            var asyncDisposableStore = store.As<IAsyncDisposable>();

            await using (new DistributedLockService(
                Mock.Of<IEventLogger<DistributedLockService>>(),
                store.Object,
                Mock.Of<ITime>()
            ).ConfigureAwait(false))
            { }

            asyncDisposableStore.Verify(s => s.DisposeAsync(), Times.Once);
        }
    }
}
