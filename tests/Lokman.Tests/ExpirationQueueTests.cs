using Xunit;
using System.Threading.Tasks;
using Xunit.Abstractions;
using System.Threading;
using System.Collections.Generic;
using Moq;
using System;

namespace Lokman.Tests
{
    public class ExpirationQueueTests
    {
        private readonly ITestOutputHelper _logger;

        public ExpirationQueueTests(ITestOutputHelper testOutput) => _logger = testOutput;

        [Fact]
        public async Task EnqueueAsync_Should_ExecuteAction()
        {
            var moment = new DateTimeOffset(2000, 1, 1, 0, 0, 0, default);
            var time = Mock.Of<ITime>(t => t.UtcNow == moment);

            using var queue = new ExpirationQueue(time, runThread: false);

            bool isActionCalled = false;
            await queue.EnqueueAsync("foo", moment.Ticks, () => isActionCalled = true, default).ConfigureAwait(false);

            var ticksToWait = queue.ThreadLoopBody(new List<(string Key, long Ticks, Action Action)>());

            Assert.True(isActionCalled);
            Assert.Equal(queue._defaultWaitTicks, ticksToWait);
        }

        [Fact]
        public async Task EnqueueAsync_Should_SetWakeUpEvent()
        {
            var moment = new DateTimeOffset(2000, 1, 1, 0, 0, 0, default);
            var time = Mock.Of<ITime>(t => t.UtcNow == moment);

            var queue = new Mock<ExpirationQueue>(time, false) {
                CallBase = true,
            };

            await queue.Object.EnqueueAsync("foo", moment.Ticks, () => { }, default).ConfigureAwait(false);

            queue.Verify(q => q.SetWakeUpEvent(), Times.Once);
        }

        [Fact]
        public async Task EnqueueAsync_Should_ThrowIfObjectIsDisposed()
        {
            var queue = new ExpirationQueue(Mock.Of<ITime>(), runThread: false);
            queue.Dispose();
            await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
                await queue.EnqueueAsync("foo", 123, () => { }, default).ConfigureAwait(false)
            ).ConfigureAwait(false);
        }

        [Fact]
        public async Task DequeueAsync_Should_ThrowIfObjectIsDisposed()
        {
            var queue = new ExpirationQueue(Mock.Of<ITime>(), runThread: false);
            queue.Dispose();
            await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
                await queue.DequeueAsync("foo", default).ConfigureAwait(false)
            ).ConfigureAwait(false);
        }

        [Fact]
        public async Task UpdateExpirationAsync_Should_ThrowIfObjectIsDisposed()
        {
            var queue = new ExpirationQueue(Mock.Of<ITime>(), runThread: false);
            queue.Dispose();
            await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
                await queue.UpdateExpirationAsync("foo", 123, default).ConfigureAwait(false)
            ).ConfigureAwait(false);
        }
    }
}
