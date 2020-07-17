using System.Runtime.CompilerServices;
using Grpc.Core;

namespace Lokman
{
    public static class GrpcExtensions
    {
        public static ConfiguredTaskAwaitable<TResponse> ConfigureAwait<TResponse>(this AsyncUnaryCall<TResponse> call, bool continueOnCapturedContext)
            => call.ResponseAsync.ConfigureAwait(continueOnCapturedContext);
    }
}
