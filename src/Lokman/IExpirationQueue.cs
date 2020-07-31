using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
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

#pragma warning disable MA0045 // Do not use blocking call (make method async) | we use dedicated thread
#pragma warning disable MA0055 // Do not use destructor | have unmanaged objects
    public class ExpirationQueue : IExpirationQueue, IAsyncDisposable, IDisposable
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

        /// I think <see cref="List{T}"/> + <see cref="SemaphoreSlim"/> will be better
        /// than a <see cref="System.Collections.Concurrent.ConcurrentBag{T}"/> but maybe we need prove it with benchmark
        internal readonly List<ExpirationRecord> _actions = new List<ExpirationRecord>();
        private readonly List<ExpirationRecord> _enqueueSet = new List<ExpirationRecord>();
        private readonly Dictionary<string, long> _updateSet = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _deleteSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private readonly SemaphoreSlim _enqueueLock = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _updateLock = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _deleteLock = new SemaphoreSlim(1, 1);

        private readonly ManualResetEventSlim _wakeupEvent = new ManualResetEventSlim(initialState: false);

        private readonly Predicate<ExpirationRecord> _cachedDeletePredicate;

        public ExpirationQueue() : this(SystemTime.Instance, runThread: true) { }

        // for testing
        internal ExpirationQueue(ITime time, bool runThread)
        {
            _time = time;
            _cancellationTokenSource = new CancellationTokenSource();
            _cachedDeletePredicate = tuple => _deleteSet.Any(keyToDelete => string.Equals(keyToDelete, tuple.Key, StringComparison.OrdinalIgnoreCase));

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
                catch (OperationCanceledException)
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
            ProcessSets();

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
                    Debug.WriteLine($"Action of ({key}, {ticks.ToString(CultureInfo.InvariantCulture)}) throws exception: {ex}");
                }
            }

            // deleting the actions that have already been fired (actions is sorted list, so this is correct)
            if (i > 0)
                _actions.RemoveRange(0, i);

            return ticksToNextFire;
        }

        private void ProcessSets()
        {
            if (_cancellationTokenSource.Token.IsCancellationRequested)
                return;

            _enqueueLock.Wait(_cancellationTokenSource.Token);
            try
            {
                if (_enqueueSet.Count > 0)
                {
                    _actions.AddRange(_enqueueSet);
                    _enqueueSet.Clear();
                    _wakeupEvent.Reset();
                }
            }
            finally
            {
                _enqueueLock.Release();
            }

            _deleteLock.Wait(_cancellationTokenSource.Token);
            try
            {
                _actions.RemoveAll(_cachedDeletePredicate);
            }
            finally
            {
                _deleteLock.Release();
            }

            _updateLock.Wait(_cancellationTokenSource.Token);
            try
            {
                if (_updateSet.Count > 0)
                {
                    foreach (var (key, newTicks) in _updateSet)
                    {
                        for (int j = 0; j < _actions.Count; j++)
                        {
                            var item = _actions[j];
                            if (!string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase))
                                continue;
                            item.Ticks = newTicks;
                        }
                    }
                    _updateSet.Clear();
                    _wakeupEvent.Reset();
                }
            }
            finally
            {
                _updateLock.Release();
            }
            _actions.Sort();
        }

        public async ValueTask EnqueueAsync(string key, long ticks, Action action, CancellationToken cancellationToken = default)
        {
            if (_isDisposed)
                ThrowHelper.ObjectDisposedException(nameof(ExpirationQueue));

            await _enqueueLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                _enqueueSet.Add((key, ticks, action));
                SetWakeUpEvent();
            }
            finally
            {
                _enqueueLock.Release();
            }
        }

        public async ValueTask DequeueAsync(string key, CancellationToken cancellationToken = default)
        {
            if (_isDisposed)
                ThrowHelper.ObjectDisposedException(nameof(ExpirationQueue));

            await _deleteLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                _deleteSet.Add(key);
            }
            finally
            {
                _deleteLock.Release();
            }
        }

        public async ValueTask UpdateExpirationAsync(string key, long ticks, CancellationToken cancellationToken = default)
        {
            if (_isDisposed)
                ThrowHelper.ObjectDisposedException(nameof(ExpirationQueue));

            await _updateLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                _updateSet[key] = ticks;
                SetWakeUpEvent();
            }
            finally
            {
                _updateLock.Release();
            }
        }

        protected virtual ValueTask DisposeAsyncCore() => default;

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                StopThread();

                if (disposing)
                {
                    _wakeupEvent.Dispose();
                    _enqueueLock.Dispose();
                    _cancellationTokenSource.Dispose();
                }
                _isDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore().ConfigureAwait(false);
            Dispose(disposing: false);
            GC.SuppressFinalize(this);
        }

        ~ExpirationQueue() => Dispose(disposing: false);

        // for testing
        internal virtual void SetWakeUpEvent() => _wakeupEvent.Set();
        internal virtual void SpinWait(int iterations) => Thread.SpinWait(iterations);
        internal virtual void WaitWithWakeup(TimeSpan interval) => _wakeupEvent.Wait(interval, _cancellationTokenSource.Token);
        internal virtual void StopThread()
        {
            if (!_cancellationTokenSource.IsCancellationRequested)
                _cancellationTokenSource.Cancel();
        }

        internal class ExpirationRecord : IComparable<ExpirationRecord>, IEquatable<ExpirationRecord>, IComparable
        {
            public string Key;
            public long Ticks;
            public Action Action;

            public static implicit operator ExpirationRecord((string Key, long Ticks, Action Action) value)
                => new ExpirationRecord(value.Key, value.Ticks, value.Action);
            public ExpirationRecord(string key, long ticks, Action action) => (Key, Ticks, Action) = (key, ticks, action);
            public int CompareTo(ExpirationRecord? record) => record is null ? -1 : Ticks.CompareTo(record.Ticks);
            public int CompareTo(object? obj) => CompareTo(obj as ExpirationRecord);
            public void Deconstruct(out string key, out long ticks, out Action action) => (key, ticks, action) = (Key, Ticks, Action);
            public override bool Equals(object? obj) => Equals(obj as ExpirationRecord);
            public bool Equals([AllowNull] ExpirationRecord other) => other != null && string.Equals(Key, other.Key, StringComparison.OrdinalIgnoreCase) && Ticks == other.Ticks;
            public override int GetHashCode() => HashCode.Combine(Key, Ticks);
            public override string ToString() => $"{Key ?? "null"} - {Ticks.ToString(CultureInfo.InvariantCulture)}";
        }
    }
}
