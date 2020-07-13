using System;
using System.Threading.Tasks;
using Moq;
using FluentAssertions;
using Xunit;

namespace Lokman.Tests
{
    public class DistributedLockHandleTests
    {
        [Fact]
        public async Task DisposeAsync_Should_RunReturnAsync()
        {
            var lockObj = new Mock<IDistributedLock>();
            await using (var _ = new DistributedLockHandle(lockObj.Object).ConfigureAwait(false))
            { }
            lockObj.Verify(l => l.ReturnAsync(It.IsAny<DistributedLockHandle>()), Times.Once);
        }

        [Fact]
        public async Task Clear_Should_ReturnDefaultObject()
        {
            var lockObj = new DistributedLockHandle(Mock.Of<IDistributedLock>());
            var defaultObj = new DistributedLockHandle(Mock.Of<IDistributedLock>());
            await using var _ = lockObj.ConfigureAwait(false);
            lockObj._id = Guid.NewGuid();
            lockObj._resources = "foo";

            lockObj.Clear();

            lockObj.Should().BeEquivalentTo(defaultObj, $"'{nameof(DistributedLockHandle.Clear)}' should clean fields");
        }
    }
}
