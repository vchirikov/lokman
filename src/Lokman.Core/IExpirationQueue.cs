using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace Lokman
{
    public interface IExpirationQueue
    {
        ValueTask EnqueueAsync(string key, long ticks, Action action, CancellationToken cancellationToken = default);
        ValueTask DequeueAsync(string key, CancellationToken cancellationToken = default);
        ValueTask UpdateExpirationAsync(string key, long ticks, CancellationToken cancellationToken = default);
    }

    public class ExpirationQueue : IDisposable, IExpirationQueue
    {
        /// <summary>
        /// The max ticks between action and current time, when we will use <see cref="Thread.SpinWait(int)"/>
        /// After the threshold we will use <see cref="ManualResetEventSlim.Wait(TimeSpan, CancellationToken)"/>
        /// <seealso cref="ThreadEntryPoint"/>
        /// </summary>
        public long SpinWaitMaxTicksThreshold = TimeSpan.FromSeconds(1).Ticks;

        /// <summary>
        /// The number of iterations that will be used in <seealso cref="Thread.SpinWait(int)"/>
        /// if current ticks are less than <see cref="SpinWaitMaxTicksThreshold"/>
        /// Default value is around 0.03 ms
        /// <seealso cref="ThreadEntryPoint"/>
        /// </summary>
        public int SpinWaitIterations = 1_000;

        private readonly ITime _time;
        private readonly Thread? _workerThread;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private bool _isDisposed;

        private readonly List<(string Key, long Ticks, Action Action)> _actions
            = new List<(string Key, long Ticks, Action Action)>();

        private readonly List<(string Key, long Ticks, Action Action)> _pendingActions
            = new List<(string Key, long Ticks, Action Action)>();

        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private readonly ManualResetEventSlim _wakeupEvent = new ManualResetEventSlim(false);

        private static readonly Comparer<(string Key, long Ticks, Action Action)> _comparer
            = Comparer<(string Key, long Ticks, Action Action)>.Create((t1, t2) => unchecked((int)(t1.Ticks - t2.Ticks)));

        // for testing
        internal ExpirationQueue(ITime time, bool runThread)
        {
            _time = time;
            _cancellationTokenSource = new CancellationTokenSource();
            if (runThread)
            {
                _workerThread = new Thread(ThreadEntryPoint) {
                    IsBackground = true,
                    CurrentCulture = CultureInfo.InvariantCulture,
                    CurrentUICulture = CultureInfo.InvariantCulture,
                    Name = $"{nameof(ExpirationQueue)}_{Guid.NewGuid():n}",
                };
                _workerThread.Start();
            }
        }

        internal void ThreadEntryPoint()
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    long ticksToNextFire = ThreadLoopBody();
                    if (ticksToNextFire < 0)
                    {
                        WaitWithWakeup(TimeSpan.FromMilliseconds(-1));
                    }
                    else if (ticksToNextFire > SpinWaitMaxTicksThreshold)
                    {
                        var ticksToStartSpinWait = ticksToNextFire - SpinWaitMaxTicksThreshold + 1;
                        WaitWithWakeup(TimeSpan.FromTicks(ticksToStartSpinWait));
                    }
                    else
                    {
                        SpinWait(SpinWaitIterations);
                    }
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Returns tick count from current moment to the next action,
        /// or <c>-1</c> if actions count is <c>0</c>
        /// </summary>
        internal virtual long ThreadLoopBody()
        {
            ProcessPendingActions();

            long ticksToNextFire = -1;
            var count = _actions.Count;
            if (count == 0)
                return ticksToNextFire;

            int i = 0;
            var currentTicks = _time.UtcNow.UtcTicks;

            for (; i < count; ++i)
            {
                var (key, ticks, action) = _actions[i];
                // if this action ticks in the future
                if (currentTicks < ticks)
                {
                    ticksToNextFire = ticks - currentTicks;
                    break;
                }
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    // ToDo: add a better logging here
                    Debug.WriteLine($"Action of ({key}, {ticks}) throws exception: {ex}");
                }
            }

            // deleting the actions that have already been fired (actions is sorted list, so this is correct)
            if (i > 0)
                _actions.RemoveRange(0, i);

            return ticksToNextFire;

            void ProcessPendingActions()
            {
                if (_cancellationTokenSource.Token.IsCancellationRequested)
                    return;

                _lock.Wait(_cancellationTokenSource.Token);
                try
                {
                    if (_pendingActions.Count > 0)
                    {
                        _actions.AddRange(_pendingActions);
                        _pendingActions.Clear();
                        _wakeupEvent.Reset();
                    }
                }
                finally
                {
                    _lock.Release();
                }
                _actions.Sort(_comparer);
            }
        }

        public async ValueTask EnqueueAsync(string key, long ticks, Action action, CancellationToken cancellationToken = default)
        {
            if (_isDisposed)
                ThrowHelper.ObjectDisposedException(nameof(ExpirationQueue));

            await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                _pendingActions.Add((key, ticks, action));
                SetWakeUpEvent();
            }
            finally
            {
                _lock.Release();
            }
        }

        public ValueTask DequeueAsync(string key, CancellationToken cancellationToken = default)
        {
            if (_isDisposed)
                ThrowHelper.ObjectDisposedException(nameof(ExpirationQueue));

            throw new NotImplementedException();
        }

        public ValueTask UpdateExpirationAsync(string key, long ticks, CancellationToken cancellationToken = default)
        {
            if (_isDisposed)
                ThrowHelper.ObjectDisposedException(nameof(ExpirationQueue));

            throw new NotImplementedException();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
                return;

            if (disposing)
            {
                _wakeupEvent.Dispose();
                _lock.Dispose();
            }

            // we want to break the loop in the worker thread if we're called from the finalizer
            if (!_cancellationTokenSource.IsCancellationRequested)
                _cancellationTokenSource.Cancel();

            _cancellationTokenSource.Dispose();
            _isDisposed = true;
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        ~ExpirationQueue() => Dispose(disposing: false);

        // for testing
        internal virtual void SetWakeUpEvent() => _wakeupEvent.Set();
        internal virtual void SpinWait(int iterations) => Thread.SpinWait(iterations);
        internal virtual void WaitWithWakeup(TimeSpan interval) => _wakeupEvent.Wait(interval, _cancellationTokenSource.Token);
    }
}
