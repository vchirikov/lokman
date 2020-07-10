using System.Threading;
using System.Threading.Tasks;

namespace Lokman
{
    public interface IDistributedLockService
    {
        ValueTask<OperationResult<LockResponse, Error>> ProcessRequestAsync(LockRequest request, CancellationToken cancellationToken = default);
    }
}
