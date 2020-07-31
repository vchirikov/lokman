using System;
using System.Threading.Tasks;
using Moq;
using FluentAssertions;
using Xunit;
using System.Collections.Generic;
using System.Threading;

namespace Lokman.UnitTests
{
    public class DistributedLockHandleTests
    {
        [Fact]
        public async Task DisposeAsync_Should_RunReleaseAsyncInReverseOrder()
        {
            var lockObj = new Mock<IDistributedLock>();
            var sequence = new List<long>(4);
            lockObj.Setup(l => l.ReleaseAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .Callback((string key, long token, CancellationToken cancellationToken) => { sequence.Add(token); });

            var handle = new DistributedLockHandle(lockObj.Object);
            await using (handle.ConfigureAwait(false))
            {
                handle._records.Add(("resource1", 1));
                handle._records.Add(("resource2", 2));
                handle._records.Add(("resource3", 3));
            }

            lockObj.Verify(l => l.ReleaseAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
            Assert.Equal(3, sequence[0]);
            Assert.Equal(2, sequence[1]);
            Assert.Equal(1, sequence[2]);
        }

        [Fact]
        public async Task DisposeAsync_Should_RunReturnAsync()
        {
            var lockObj = new Mock<IDistributedLock>();
            await using (var _ = new DistributedLockHandle(lockObj.Object).ConfigureAwait(false))
            { }
            lockObj.Verify(l => l.ReturnAsync(It.IsAny<DistributedLockHandle>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_Should_RunUpdateAsync()
        {
            var lockObj = new Mock<IDistributedLock>();
            var sequence = new List<long>(4);
            lockObj.Setup(l => l.UpdateAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .Callback((string key, long token, TimeSpan duration, CancellationToken cancellationToken) => { sequence.Add(token); });

            var handle = new DistributedLockHandle(lockObj.Object);
            OperationResult<DistributedLockHandle, Error> result = default;
            await using (handle.ConfigureAwait(false))
            {
                handle._records.Add(("resource1", 1));
                handle._records.Add(("resource2", 2));
                handle._records.Add(("resource3", 3));
                result = await handle.UpdateAsync(TimeSpan.MaxValue).ConfigureAwait(false);
            }

            lockObj.Verify(l => l.UpdateAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
            Assert.True(result.IsSuccess);
            Assert.Same(result.Value, handle);
            Assert.Equal(1, sequence[0]);
            Assert.Equal(2, sequence[1]);
            Assert.Equal(3, sequence[2]);
        }

        [Fact]
        public async Task Clear_Should_ReturnDefaultObject()
        {
            var lockObj = new DistributedLockHandle(Mock.Of<IDistributedLock>());
            var defaultObj = new DistributedLockHandle(Mock.Of<IDistributedLock>());
            await using var _ = lockObj.ConfigureAwait(false);
            lockObj._records.Add(("resource1", 1));

            lockObj.Clear();

            lockObj.Should().BeEquivalentTo(defaultObj, $"'{nameof(DistributedLockHandle.Clear)}' should clean fields");
        }
    }
}
