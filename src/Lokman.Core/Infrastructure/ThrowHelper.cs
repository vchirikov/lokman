using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Lokman
{
    internal static class ThrowHelper
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void InvalidOperationException(string? message = null)
        {
            if (message is null)
                throw new InvalidOperationException();
            else
                throw new InvalidOperationException(message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static T InvalidOperationException<T>(string? message = null)
        {
            InvalidOperationException(message);
            return default!;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static T NotSupportedException<T>() => throw new NotSupportedException();

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ArgumentNullException(string paramName) => throw new ArgumentNullException(paramName);

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TaskCanceledException() => throw new TaskCanceledException();

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ArgumentOutOfRangeException(string param, object value, string message) => throw new ArgumentOutOfRangeException(param, value, message);

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ArgumentException(string message) => throw new ArgumentException(message);

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ObjectDisposedException(string objectName) => throw new ObjectDisposedException(objectName);

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void KeyNotFoundException(string message) => throw new KeyNotFoundException(message);
    }
}
