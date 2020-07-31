using Xunit;
using System.Threading.Tasks;
using Moq;
using Lokman.Protos;
using System;
using Google.Protobuf.WellKnownTypes;

namespace Lokman.UnitTests
{
    public class GrpcDistributedLockStoreTests
    {
        [Fact]
        public async Task AcquireAsync_Should_CreateAcquireLockRequest()
        {
            var grpc = new Mock<Protos.DistributedLockService.DistributedLockServiceClient>() {
                CallBase = true,
            };
            grpc.Setup(g => g.LockAsync(It.IsAny<LockRequest>(), It.IsAny<Grpc.Core.CallOptions>())).Returns(
                new Grpc.Core.AsyncUnaryCall<LockResponse>(Task.FromResult(new LockResponse() {
                    Key = "foo",
                    Token = 1,
                }), default, default, default, default));

            var store = new GrpcDistributedLockStore(grpc.Object);

            await store.AcquireAsync("foo", TimeSpan.FromSeconds(10)).ConfigureAwait(false);

            grpc.Verify(g => g.LockAsync(It.Is<LockRequest>(r =>
                r.Duration.Seconds == 10 &&
                r.Key == "foo" &&
                r.Token == -1), It.IsAny<Grpc.Core.CallOptions>()), Times.Once);
        }
        [Fact]
        public async Task ReleaseAsync_Should_CreateReleaseLockRequest()
        {
            var grpc = new Mock<Protos.DistributedLockService.DistributedLockServiceClient>() {
                CallBase = true,
            };
            grpc.Setup(g => g.LockAsync(It.IsAny<LockRequest>(), It.IsAny<Grpc.Core.CallOptions>())).Returns(
                new Grpc.Core.AsyncUnaryCall<LockResponse>(Task.FromResult(new LockResponse() {
                    Key = "foo",
                    Token = 1,
                }), default, default, default, default));

            var store = new GrpcDistributedLockStore(grpc.Object);

            await store.ReleaseAsync("foo", 1).ConfigureAwait(false);

            grpc.Verify(g => g.LockAsync(It.Is<LockRequest>(r =>
                r.Duration.Seconds == 0 &&
                r.Duration.Nanos == 0 &&
                r.Key == "foo" &&
                r.Token == 1), It.IsAny<Grpc.Core.CallOptions>()), Times.Once);

        }
        [Fact]
        public async Task UpdateAsync_Should_CreateUpdateLockRequest()
        {
            var grpc = new Mock<Protos.DistributedLockService.DistributedLockServiceClient>() {
                CallBase = true,
            };
            grpc.Setup(g => g.LockAsync(It.IsAny<LockRequest>(), It.IsAny<Grpc.Core.CallOptions>())).Returns(
                new Grpc.Core.AsyncUnaryCall<LockResponse>(Task.FromResult(new LockResponse() {
                    Key = "foo",
                    Token = 1,
                }), default, default, default, default));
            var store = new GrpcDistributedLockStore(grpc.Object);

            await store.UpdateAsync("foo", 123, TimeSpan.FromSeconds(20)).ConfigureAwait(false);

            grpc.Verify(g => g.LockAsync(It.Is<LockRequest>(r =>
                r.Duration.Seconds == 20 &&
                r.Key == "foo" &&
                r.Token == 123), It.IsAny<Grpc.Core.CallOptions>()), Times.Once);
        }
    }
}
