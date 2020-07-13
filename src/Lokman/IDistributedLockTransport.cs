using System;
using System.Threading;
using Grpc.Core;

namespace Lokman
{
    public interface IDistributedLockTransport
    {
    }

    public class GrpcDistributedLockTransport: IDistributedLockTransport
    {
        private readonly Protos.DistributedLockService.DistributedLockServiceClient _grpc;

        public GrpcDistributedLockTransport(Protos.DistributedLockService.DistributedLockServiceClient grpc)
        {
            _grpc = grpc;
        }


        public void Do(CancellationToken cancellationToken)
        {

            using var endpoint = _grpc.Lock(cancellationToken: cancellationToken);
            //endpoint.RequestStream.WriteAsync();
            endpoint.ResponseStream.ReadAllAsync()

        }

    }
}