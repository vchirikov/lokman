using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Lokman.OperationResultHelpers;

namespace Lokman
{
    /// <summary>
    /// Result of operation (without Error field or Value field)
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    public readonly struct OperationResult : IEquatable<OperationResult>
    {
        public readonly bool IsSuccess;
        public bool IsError => !IsSuccess;

        private OperationResult(bool isSuccess) => IsSuccess = isSuccess;

        public static implicit operator bool(OperationResult result) => result.IsSuccess;
        public static implicit operator OperationResult(bool result) => new OperationResult(result);

        public static bool operator ==(OperationResult left, OperationResult right) => left.Equals(right);

        public static bool operator !=(OperationResult left, OperationResult right) => !(left == right);

        public override bool Equals(object? obj) => obj is OperationResult result && Equals(result);

        public bool Equals(OperationResult other) => IsSuccess == other.IsSuccess;

        public override int GetHashCode() => IsSuccess.GetHashCode();

        // produces better IL
        private static readonly OperationResult _okResultCached = new OperationResult(isSuccess: true);
        private static readonly OperationResult _errorResultCached = new OperationResult(isSuccess: false);

        public static implicit operator OperationResult(Success _) => _okResultCached;

        public static implicit operator OperationResult(Failure _) => _errorResultCached;
    }

    /// <summary>
    /// Result of operation (without Error field)
    /// </summary>
    /// <typeparam name="TResult">Type of <see cref="Value"/> field</typeparam>
    [StructLayout(LayoutKind.Auto)]
    public readonly struct OperationResult<TResult> : IEquatable<OperationResult<TResult>>
    {
        public readonly TResult Value;

        public readonly bool IsSuccess;
        public bool IsError => !IsSuccess;

        public OperationResult(bool isSuccess)
        {
            IsSuccess = isSuccess;
            Value = default!;
        }

        public OperationResult(TResult result)
        {
            IsSuccess = true;
            Value = result;
        }

        public void Deconstruct(out bool isSuccess, out TResult value)
        {
            isSuccess = IsSuccess;
            value = Value;
        }

        public override bool Equals(object? obj) => obj is OperationResult<TResult> result && Equals(result);

        public bool Equals(OperationResult<TResult> other)
            => EqualityComparer<TResult>.Default.Equals(Value, other.Value)
            && IsSuccess == other.IsSuccess;

        public override int GetHashCode() => HashCode.Combine(Value, IsSuccess);

        public static implicit operator bool(OperationResult<TResult> result) => result.IsSuccess;

        public static implicit operator OperationResult<TResult>(bool result) => new OperationResult<TResult>(result);

        public static implicit operator OperationResult<TResult>(TResult result)
            => new OperationResult<TResult>(result);

        public static bool operator ==(OperationResult<TResult> left, OperationResult<TResult> right)
            => left.Equals(right);

        public static bool operator !=(OperationResult<TResult> left, OperationResult<TResult> right)
            => !(left == right);

        // support Ok / Error

        private static readonly OperationResult<TResult> _emptyErrorResultCached
            = new OperationResult<TResult>(isSuccess: false);

        private static readonly OperationResult<TResult> _emptyOkResultCached
            = new OperationResult<TResult>(isSuccess: true);

        public static implicit operator OperationResult<TResult>(Failure _) => _emptyErrorResultCached;

        public static implicit operator OperationResult<TResult>(Success _) => _emptyOkResultCached;

        public static implicit operator OperationResult<TResult>(Success<TResult> result)
            => new OperationResult<TResult>(result._value);
    }

    /// <summary>
    /// Result of operation with <see cref="Error"/> and <see cref="Value"/> information
    /// </summary>
    /// <typeparam name="TResult">Type of Value field</typeparam>
    /// <typeparam name="TError">Type of Error field</typeparam>
    [StructLayout(LayoutKind.Auto)]
    public readonly struct OperationResult<TResult, TError> : IEquatable<OperationResult<TResult, TError>>
    {
        public readonly TResult Value;
        public readonly TError Error;

        public readonly bool IsSuccess;
        public bool IsError => !IsSuccess;

        public OperationResult(bool isSuccess)
        {
            IsSuccess = isSuccess;
            Error = default!;
            Value = default!;
        }

        public OperationResult(TResult result)
        {
            IsSuccess = true;
            Error = default!;
            Value = result;
        }

        public OperationResult(TError error)
        {
            IsSuccess = false;
            Error = error;
            Value = default!;
        }

        public void Deconstruct(out bool isSuccess, out TResult value, out TError error)
        {
            isSuccess = IsSuccess;
            value = Value;
            error = Error;
        }

        public override bool Equals(object? obj) => obj is OperationResult<TResult, TError> result && Equals(result);

        public bool Equals(OperationResult<TResult, TError> other)
        {
            return EqualityComparer<TResult>.Default.Equals(Value, other.Value) &&
                   EqualityComparer<TError>.Default.Equals(Error, other.Error) &&
                   IsSuccess == other.IsSuccess;
        }

        public override int GetHashCode() => HashCode.Combine(Value, Error, IsSuccess);

        public static implicit operator bool(OperationResult<TResult, TError> result)
            => result.IsSuccess;

        public static implicit operator OperationResult<TResult, TError>(bool result)
            => new OperationResult<TResult, TError>(result);

        public static implicit operator OperationResult<TResult, TError>(TResult result)
            => new OperationResult<TResult, TError>(result);

        public static implicit operator OperationResult<TResult, TError>(TError error)
            => new OperationResult<TResult, TError>(error);

        public static bool operator ==(OperationResult<TResult, TError> left, OperationResult<TResult, TError> right)
            => left.Equals(right);

        public static bool operator !=(OperationResult<TResult, TError> left, OperationResult<TResult, TError> right)
            => !(left == right);

        // support Ok / Error

        private static readonly OperationResult<TResult, TError> _emptyErrorResultCached
            = new OperationResult<TResult, TError>(isSuccess: false);

        private static readonly OperationResult<TResult, TError> _emptyOkResultCached
            = new OperationResult<TResult, TError>(isSuccess: true);

        public static implicit operator OperationResult<TResult, TError>(Failure _) => _emptyErrorResultCached;
        public static implicit operator OperationResult<TResult, TError>(Success _) => _emptyOkResultCached;

        public static implicit operator OperationResult<TResult, TError>(Success<TResult> result)
            => new OperationResult<TResult, TError>(result._value);

        public static implicit operator OperationResult<TResult, TError>(Failure<TError> result)
            => new OperationResult<TResult, TError>(result._error);
    }

    public static class OperationResultHelpers
    {
        // cache produces better IL / `ldfsfld + ret` vs `ldc.i4 + newobj + ret`
        private static readonly Success _successCached = new Success();
        private static readonly Failure _failureCached = new Failure();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Success Ok() => _successCached;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Failure Error() => _failureCached;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Success<TResult> Ok<TResult>(TResult result) => new Success<TResult>(result);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Failure<TError> Error<TError>(TError error) => new Failure<TError>(error);

        #region General Error support
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Failure<Error> Error(Exception ex) => new Failure<Error>(new Error(
                errorCode: default,
                SystemTime.Instance.UtcNow,
                exception: ex,
                description: ex.Message));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Failure<Error> Error(uint errorCode) => new Failure<Error>(new Error(
                errorCode: errorCode,
                SystemTime.Instance.UtcNow,
                exception: default,
                description: "Error code is " + errorCode.ToString(CultureInfo.InvariantCulture)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Failure<Error> Error(Exception ex, uint errorCode) => new Failure<Error>(new Error(
                errorCode: errorCode,
                SystemTime.Instance.UtcNow,
                exception: ex,
                description: "Error code is " + errorCode.ToString(CultureInfo.InvariantCulture)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Failure<Error> Error(Exception ex, uint errorCode, string description) => new Failure<Error>(new Error(
                errorCode: errorCode,
                SystemTime.Instance.UtcNow,
                exception: ex,
                description: description));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Failure<Error> Error(Exception ex, string description) => new Failure<Error>(new Error(
                errorCode: default,
                SystemTime.Instance.UtcNow,
                exception: ex,
                description: description));

        #endregion General Error support

        // the helper types are used for better readability
        // with non-genegic Failure we can cache empty/error result in OperationResult<TResult>, and use Error(); with OperationResult<TResult> for example
        [StructLayout(LayoutKind.Auto)]
        public readonly struct Failure { }
        [StructLayout(LayoutKind.Auto)]
        public readonly struct Failure<TError> : IEquatable<Failure<TError>>
        {
            internal readonly TError _error;
            internal Failure(TError error) => _error = error;

            public override bool Equals(object? obj) => obj is Failure<TError> failure && Equals(failure);

            public bool Equals(Failure<TError> other) => EqualityComparer<TError>.Default.Equals(_error, other._error);

            public override int GetHashCode() => HashCode.Combine(_error);

            public static bool operator ==(Failure<TError> left, Failure<TError> right) => left.Equals(right);

            public static bool operator !=(Failure<TError> left, Failure<TError> right) => !(left == right);
        }
        [StructLayout(LayoutKind.Auto)]
        public readonly struct Success { }

        [StructLayout(LayoutKind.Auto)]
        public readonly struct Success<TResult> : IEquatable<Success<TResult>>
        {
            internal readonly TResult _value;
            internal Success(TResult result) => _value = result;

            public override bool Equals(object? obj) => obj is Success<TResult> success && Equals(success);

            public bool Equals(Success<TResult> other) => EqualityComparer<TResult>.Default.Equals(_value, other._value);

            public override int GetHashCode() => HashCode.Combine(_value);

            public static bool operator ==(Success<TResult> left, Success<TResult> right) => left.Equals(right);

            public static bool operator !=(Success<TResult> left, Success<TResult> right) => !(left == right);
        }
    }
}
