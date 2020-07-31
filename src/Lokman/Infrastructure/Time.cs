using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Lokman
{
    // we want follow Task.Delay method declaration
    #pragma warning disable VSTHRD200 // Use "Async" suffix for async methods

    internal interface ITime
    {
        /// <inheritdoc cref="DateTimeOffset.UtcNow" />
        DateTimeOffset UtcNow { get; }

        /// <inheritdoc cref="Task.Delay(TimeSpan, CancellationToken)" />
        Task Delay(TimeSpan delay, CancellationToken cancellationToken);
    }

    [ExcludeFromCodeCoverage]
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
