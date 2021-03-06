using Xunit;
using System.Threading.Tasks;
using Lokman.Protos;
using System;
using Moq;
using System.Threading;
using Google.Protobuf.WellKnownTypes;

namespace Lokman.Tests
{
    public class GrpcDistributedLockServiceTests
    {
        [Fact]
        public async Task ProcessAsync_Should_ParseToAcquireAsync()
        {
            var store = new Mock<IDistributedLockStore>();
            var acquireRequest = new LockRequest() {
                Duration = Duration.FromTimeSpan(TimeSpan.FromTicks(100)),
                Token = -1,
                Key = "foo",
            };

            var service = new GrpcDistributedLockService(Mock.Of<IEventLogger<GrpcDistributedLockService>>(),
                store.Object
            );

            await service.ProcessAsync(acquireRequest, default).ConfigureAwait(false);

            store.Verify(s => s.AcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ProcessAsync_Should_ParseToUpdateAsync()
        {
            var store = new Mock<IDistributedLockStore>();

            var acquireRequest = new LockRequest() {
                Duration = Duration.FromTimeSpan(TimeSpan.FromTicks(100)),
                Token = 1,
                Key = "foo",
            };

            var service = new GrpcDistributedLockService(Mock.Of<IEventLogger<GrpcDistributedLockService>>(),
                store.Object
            );

            await service.ProcessAsync(acquireRequest, default).ConfigureAwait(false);

            store.Verify(s => s.UpdateAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ProcessAsync_Should_ParseToReleaseAsync_If_ExpirationIsZero()
        {
            var store = new Mock<IDistributedLockStore>();

            var acquireRequest = new LockRequest() {
                Duration = Duration.FromTimeSpan(default),
                Token = 0,
                Key = "foo",
            };

            var service = new GrpcDistributedLockService(Mock.Of<IEventLogger<GrpcDistributedLockService>>(),
                store.Object
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
                Duration = Duration.FromTimeSpan(default),
                Token = 1,
                Key = "foo",
            };

            var service = new GrpcDistributedLockService(Mock.Of<IEventLogger<GrpcDistributedLockService>>(),
                store.Object
            );

            await service.ProcessAsync(acquireRequest, default).ConfigureAwait(false);

            store.Verify(s => s.ReleaseAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
