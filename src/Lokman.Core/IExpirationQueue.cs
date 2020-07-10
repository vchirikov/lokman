using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lokman
{
    public interface IExpirationQueue
    {
        ValueTask EqueueAsync(long ticks, Action action, CancellationToken cancellationToken = default);
    }
}
