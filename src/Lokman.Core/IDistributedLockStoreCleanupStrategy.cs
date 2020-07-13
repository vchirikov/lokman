using System.Threading;
using System.Threading.Tasks;

namespace Lokman
{
    /// <summary>
    /// Specifies how and when to cleanup <see cref="IDistributedLockService"/>.
    /// </summary>
    public interface IDistributedLockStoreCleanupStrategy
    {
        /// <summary>
        /// Collects garbage from <paramref name="distributedLockStore"/>
        /// Runnned on each call of <see cref="IDistributedLockStore.AcquireAsync"/>.
        /// </summary>
        ValueTask CleanupAsync(IDistributedLockStore distributedLockStore, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// <inheritdoc cref="IDistributedLockStoreCleanupStrategy"/>
    /// No Operation strategy.
    /// </summary>
    public class NoOpDistributedLockStoreCleanupStrategy : IDistributedLockStoreCleanupStrategy
    {
        public static NoOpDistributedLockStoreCleanupStrategy Instance { get; } = new NoOpDistributedLockStoreCleanupStrategy();

        /// <inheritdoc />
        public ValueTask CleanupAsync(IDistributedLockStore distributedLockStore, CancellationToken cancellationToken = default)
            => new ValueTask();
    }
}
