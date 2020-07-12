using Xunit;
using System.Threading.Tasks;
using Xunit.Abstractions;
using System.Threading;
using System.Collections.Generic;
using Moq;
using System;
using System.Linq;

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

            var ticksToWait = queue.ThreadLoopBody();

            Assert.True(isActionCalled);
            Assert.Equal(-1, ticksToWait);
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

        [Fact]
        public async Task ThreadEntryPoint_IntegrationTest()
        {
            var iterations = 5;
            var time = new Mock<ITime>();
            long prevTicks = 100;
            var timeSequence = new Queue<long>();
            timeSequence.Enqueue(prevTicks);
            time.Setup(t => t.UtcNow).Returns(() => {
                if (!timeSequence.TryDequeue(out var val))
                    val = prevTicks;
                _logger.WriteLine($"Global time tick: {val}");
                prevTicks = val;
                return new DateTimeOffset(val, TimeSpan.Zero);
            });

            var queue = new Mock<ExpirationQueue>(time.Object, false) {
                CallBase = true,
            };

            queue.Object.SpinWaitMaxTicksThreshold = 100;
            queue.Object.SpinWaitIterations = 30 * 3;

            _logger.WriteLine($"{nameof(ExpirationQueue.SpinWaitMaxTicksThreshold)} = {queue.Object.SpinWaitMaxTicksThreshold}");
            _logger.WriteLine($"{nameof(ExpirationQueue.SpinWaitIterations)} = {queue.Object.SpinWaitIterations} ticks: ~{queue.Object.SpinWaitIterations / 3}");

            queue.Setup(q => q.ThreadLoopBody()).Returns(new InvocationFunc((IInvocation i) => {
                var methodInfo = i.GetType().GetMethod("CallBase", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (methodInfo == null)
                    throw new Exception("Moq internal breaking change");
                var result = methodInfo.Invoke(i, null);
                _logger.WriteLine($"{nameof(ExpirationQueue.ThreadLoopBody)} returns {result}");
                return result;
            }));

            queue.Setup(q => q.SpinWait(It.IsAny<int>())).Callback((int spinWaitIterations) => {
                _logger.WriteLine($"SpinWait {spinWaitIterations} iterations ~ {(spinWaitIterations / 3)} ticks");
                timeSequence.Enqueue(prevTicks + (spinWaitIterations / 3));
                if (0 > --iterations)
                    throw new TaskCanceledException();
            });

            var isEventWaitWithInfinityCalled = false;
            queue.Setup(q => q.WaitWithWakeup(It.IsAny<TimeSpan>())).Callback((TimeSpan waitTimeSpan) => {
                if (waitTimeSpan == TimeSpan.FromMilliseconds(-1))
                {
                    _logger.WriteLine($"Wait until enqueue..Exit");
                    isEventWaitWithInfinityCalled = true;
                    throw new TaskCanceledException();
                }
                _logger.WriteLine($"Wakeup wait {waitTimeSpan.Ticks} ticks, TimeSpan: {waitTimeSpan}");
                timeSequence.Enqueue(prevTicks + waitTimeSpan.Ticks);
                if (0 > --iterations)
                    throw new TaskCanceledException();
            });

            var isActionRunned = false;
            await queue.Object.EnqueueAsync("foo", 400, () => {
                _logger.WriteLine("Action is running");
                isActionRunned = true;
            }, default).ConfigureAwait(false);
            queue.Object.ThreadEntryPoint();

            queue.Verify(q => q.SpinWait(It.IsAny<int>()), Times.AtLeastOnce);
            queue.Verify(q => q.WaitWithWakeup(It.IsAny<TimeSpan>()), Times.AtLeastOnce);
            Assert.True(isActionRunned);
            Assert.True(isEventWaitWithInfinityCalled);
        }

        [Theory]
        [MemberData(nameof(ThreadEntryPoint_Input))]
        public void ThreadEntryPoint_Should_UseSpinWaitOrWaitOnEvent(DateTimeOffset moment, long threadLoopBodyResult, bool isSpinWaitExpected)
        {
            bool? isSpinWaitCalled = null;

            var time = Mock.Of<ITime>(t => t.UtcNow == moment);
            var queue = new Mock<ExpirationQueue>(time, false) {
                CallBase = true,
            };
            queue.Setup(q => q.ThreadLoopBody()).Returns(threadLoopBodyResult);
            queue.Setup(q => q.SpinWait(It.IsAny<int>())).Callback(() => {
                isSpinWaitCalled = true;
                throw new TaskCanceledException();
            });
            queue.Setup(q => q.WaitWithWakeup(It.IsAny<TimeSpan>())).Callback(() => {
                isSpinWaitCalled = false;
                throw new TaskCanceledException();
            });

            queue.Object.ThreadEntryPoint();

            Assert.NotNull(isSpinWaitCalled);
            Assert.Equal(isSpinWaitExpected, isSpinWaitCalled.Value);
        }

        public static IEnumerable<object[]> ThreadEntryPoint_Input()
        {
            var moment = new DateTimeOffset(2000, 1, 1, 0, 0, 0, default);
            var queue = new ExpirationQueue(Mock.Of<ITime>(), false);
            return new List<object[]> {
                new object[]{ moment,  queue.SpinWaitMaxTicksThreshold - 1, true },
                new object[]{ moment,  queue.SpinWaitMaxTicksThreshold, true },
                new object[]{ moment,  queue.SpinWaitMaxTicksThreshold + 1, false },
            };
        }
    }
}
