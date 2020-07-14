using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.ObjectPool;
using Moq;
using Xunit;

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
            await using var _ = lockObj.ConfigureAwait(false);
            lockObj._resources = new[] { "foo", "bar" };
            lockObj._duration = TimeSpan.FromSeconds(3);

            lockObj.Clear();

            lockObj.Should().BeEquivalentTo(defaultObj, "'Clear' should clean fields");
        }

        private static DistributedLock CreateLock(IDistributedLockManager mgr = null) => new DistributedLock(
            mgr ?? Mock.Of<IDistributedLockManager>(),
            new LeakTrackingObjectPoolProvider(new DefaultObjectPoolProvider()),
            Mock.Of<IDistributedLockStore>()
        );
    }
}
