using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lokman
{
    public interface IExpirationQueue
    {
        ValueTask EnqueueAsync(string key, long ticks, Action action, CancellationToken cancellationToken = default);
        ValueTask DequeueAsync(string key, CancellationToken cancellationToken = default);
    }
}
