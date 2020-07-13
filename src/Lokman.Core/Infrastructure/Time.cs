using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lokman
{
    internal interface ITime
    {
        /// <inheritdoc cref="DateTimeOffset.UtcNow" />
        DateTimeOffset UtcNow { get; }

        /// <inheritdoc cref="Task.Delay(TimeSpan, CancellationToken)" />
        Task Delay(TimeSpan delay, CancellationToken cancellationToken);
    }

    internal sealed class SystemTime : ITime
    {
        public static SystemTime Instance = new SystemTime();

        /// <inheritdoc/>
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

        /// <inheritdoc/>
        public Task Delay(TimeSpan delay, CancellationToken cancellationToken)
            => Task.Delay(delay, cancellationToken);

        private SystemTime() { }
    }
}
