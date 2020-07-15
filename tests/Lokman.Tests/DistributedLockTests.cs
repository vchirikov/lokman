using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.ObjectPool;
using Moq;
using Xunit;
using System.Threading;

namespace Lokman.Tests
{
    public class DistributedLockTests
    {
        [Fact]
        public async Task DisposeAsync_Should_RunReturnAsync()
        {
            var mgr = new Mock<IDistributedLockManager>();
            await using (var _ = CreateLock(mgr.Object).ConfigureAwait(false))
            { }
            mgr.Verify(m => m.ReturnAsync(It.IsAny<IDistributedLock>()), Times.Once);
        }

        [Fact]
        public async Task Clear_Should_ReturnDefaultObject()
        {
            var lockObj = CreateLock();
            var defaultObj = CreateLock();
            defaultObj._duration = default;
            defaultObj._resources = default;
            await using var _ = lockObj.ConfigureAwait(false);
            lockObj._resources = new[] { "foo", "bar" };
            lockObj._duration = TimeSpan.FromSeconds(3);

            lockObj.Clear();

            lockObj.Should().BeEquivalentTo(defaultObj, "'Clear' should clean fields");
        }

        [Fact]
        public async Task AcquireAsync_Should_ReleaseTakenLocksIfExceptionOccurs()
        {
            var store = new Mock<IDistributedLockStore>();
            store.SetupSequence(s => s.AcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1)
                .ReturnsAsync(2)
                .ThrowsAsync(new Exception("Something went wrong"));

            var lockObj = CreateLock(mgr: null, store.Object);
            var result = await lockObj.AcquireAsync().ConfigureAwait(false);

            store.Verify(s => s.AcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
            store.Verify(s => s.ReleaseAsync("resource1", 1, It.IsAny<CancellationToken>()), Times.Once);
            store.Verify(s => s.ReleaseAsync("resource2", 2, It.IsAny<CancellationToken>()), Times.Once);
            store.Verify(s => s.ReleaseAsync("resource3", It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);

            Assert.True(result.IsError);
            Assert.NotNull(result.Error?.Exception);
            Assert.Equal("Something went wrong", result.Error.Exception.Message);
        }

        private static DistributedLock CreateLock(IDistributedLockManager? mgr = null, IDistributedLockStore? store = null)
            => new DistributedLock(
                mgr ?? Mock.Of<IDistributedLockManager>(),
                new LeakTrackingObjectPoolProvider(new DefaultObjectPoolProvider()),
                store ?? Mock.Of<IDistributedLockStore>()
            ) {
                _resources = new string[] { "resource1", "resource2", "resource3" },
                _duration = TimeSpan.FromSeconds(10)
            };

    }
}
