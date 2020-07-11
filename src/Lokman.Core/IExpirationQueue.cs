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
        internal readonly long _defaultWaitTicks = TimeSpan.FromMilliseconds(200).Ticks;

        private readonly ITime _time;
        private readonly Thread? _workerThread;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private bool _isDisposed;

        private readonly List<(string Key, long Ticks, Action Action)> _actions
            = new List<(string Key, long Ticks, Action Action)>();

        private readonly List<(string Key, long Ticks, Action Action)> _pendingActions
            = new List<(string Key, long Ticks, Action Action)>();

        private readonly SemaphoreSlim _pendingLock = new SemaphoreSlim(1, 1);
        private ManualResetEventSlim _wakeupEvent = new ManualResetEventSlim(false);

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

        private void ThreadEntryPoint()
        {
            var toRemove = new List<(string Key, long Ticks, Action Action)>();
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                long waitTicks = ThreadLoopBody(toRemove);
                _wakeupEvent.Wait(TimeSpan.FromTicks(waitTicks), _cancellationTokenSource.Token);
            }
        }

        internal long ThreadLoopBody(List<(string Key, long Ticks, Action Action)> toRemove)
        {
            _pendingLock.Wait(_cancellationTokenSource.Token);
            try
            {
                if (_pendingActions.Count > 0)
                {
                    _actions.AddRange(_pendingActions);
                    _pendingActions.Clear();
                }
            }
            finally
            {
                _pendingLock.Release();
            }

            long waitTicks = _defaultWaitTicks;
            if (_actions.Count > 0)
            {
                var currentTicks = _time.UtcNow.UtcTicks;
                long? nextMinTicks = null;
                foreach (var tuple in _actions)
                {
                    if ((tuple.Ticks - currentTicks) <= 0)
                    {
                        toRemove.Add(tuple);
                        try
                        {
                            tuple.Action();
                        }
                        catch (Exception ex)
                        {
                            // ToDo: add logging here
                            Debug.WriteLine($"Action of ({tuple.Key}, {tuple.Ticks}) throws exception: {ex}");
                        }
                    }
                    else
                    {
                        if (nextMinTicks == null)
                            nextMinTicks = tuple.Ticks;
                        else if (nextMinTicks > tuple.Ticks)
                            nextMinTicks = tuple.Ticks;
                    }
                }
                foreach (var item in toRemove)
                    _actions.Remove(item);
                toRemove.Clear();

                if (nextMinTicks.HasValue)
                    waitTicks = nextMinTicks.Value - currentTicks;
            }

            return waitTicks;
        }

        public async ValueTask EnqueueAsync(string key, long ticks, Action action, CancellationToken cancellationToken = default)
        {
            if (_isDisposed)
                ThrowHelper.ObjectDisposedException(nameof(ExpirationQueue));

            await _pendingLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                _pendingActions.Add((key, ticks, action));
                SetWakeUpEvent();
            }
            finally
            {
                _pendingLock.Release();
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
            if (!_isDisposed)
            {
                if (disposing)
                {
                    _wakeupEvent?.Dispose();
                }
                _wakeupEvent = null!;
                if (!_cancellationTokenSource.IsCancellationRequested)
                    _cancellationTokenSource.Cancel();
                _isDisposed = true;
            }
        }

        ~ExpirationQueue()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        // for testing

        protected internal virtual void SetWakeUpEvent() => _wakeupEvent.Set();
    }
}
