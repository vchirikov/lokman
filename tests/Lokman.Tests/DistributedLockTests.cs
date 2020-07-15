using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.ObjectPool;
using Moq;
using Xunit;
using System.Threading;
using System.Collections.Generic;

namespace Lokman.Tests
{
    public class DistributedLockTests
    {
        [Fact]
        public async Task AcquireAsync_Should_UseCancellation()
        {
            var store = new Mock<IDistributedLockStore>();
            var isDelayEnded = false;
            var tokens = new Queue<long>(new long[] { 1, 2, 3 });
            store.Setup(s => s.AcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .Returns(async (string key, TimeSpan duration, CancellationToken cancellationToken) => {
                    var result = tokens.Dequeue();
                    if (result == 3)
                    {
                        // emulate long waiting on acquiring resource3
                        await Task.Delay(10_000, cancellationToken).ConfigureAwait(false);
                        isDelayEnded = true;
                    }
                    return result;
                });
            var lockObj = CreateLock(mgr: null, store.Object);
            var cts = new CancellationTokenSource();
            cts.Cancel();

            var result = await lockObj.AcquireAsync(cts.Token).ConfigureAwait(false);

            store.Verify(s => s.AcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
            store.Verify(s => s.ReleaseAsync("resource1", 1, It.IsAny<CancellationToken>()), Times.Once);
            store.Verify(s => s.ReleaseAsync("resource2", 2, It.IsAny<CancellationToken>()), Times.Once);

            Assert.True(result.IsError);
            Assert.False(isDelayEnded);
        }

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
            Assert.True(result.Error.ErrorCode > ErrorCodes.Client.UnknownError);
        }

        [Fact]
        public async Task ReleaseAsync_Should_CallReleaseAsync()
        {
            var store = new Mock<IDistributedLockStore>();
            store.Setup(s => s.ReleaseAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync(2);

            var lockObj = CreateLock(mgr: null, store.Object);
            var result = await lockObj.ReleaseAsync("foo", 1).ConfigureAwait(false);

            store.Verify(s => s.ReleaseAsync("foo", 1, It.IsAny<CancellationToken>()), Times.Once);

            Assert.True(result.IsSuccess);
            Assert.Null(result.Error);
        }

        [Fact]
        public async Task ReleaseAsync_Should_ReturnErrorIfExceptionOccurs()
        {
            var store = new Mock<IDistributedLockStore>();
            store.Setup(s => s.ReleaseAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Something went wrong"));

            var lockObj = CreateLock(mgr: null, store.Object);
            var result = await lockObj.ReleaseAsync("foo", 1).ConfigureAwait(false);

            store.Verify(s => s.ReleaseAsync("foo", 1, It.IsAny<CancellationToken>()), Times.Once);

            Assert.True(result.IsError);
            Assert.NotNull(result.Error?.Exception);
            Assert.Equal("Something went wrong", result.Error.Exception.Message);
            Assert.True(result.Error.ErrorCode > ErrorCodes.Client.UnknownError);
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
