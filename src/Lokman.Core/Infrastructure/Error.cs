using System;
using System.Collections.Generic;
using System.Globalization;

namespace Lokman
{
    public class Error
    {
        public Error(uint errorCode, DateTimeOffset dateTime, Exception? exception, string description)
        {
            ErrorCode = errorCode;
            DateTime = dateTime;
            Exception = exception ?? throw new ArgumentNullException(nameof(exception));
            Description = description ?? throw new ArgumentNullException(nameof(description));
        }

        public uint ErrorCode { get; }
        public DateTimeOffset DateTime { get; }
        public Exception? Exception { get; }
        public string Description { get; }

        public override string ToString()
            => $"[{DateTime.ToString(CultureInfo.InvariantCulture)}] ({ErrorCode}) {Description} Exception: {Exception?.ToString() ?? "null"}";
    }

    /// <summary>
    /// [0; 5000) - Server errors
    /// [5000; ) - Client errors
    /// </summary>
    public static class ErrorCodes
    {
        /// <summary>
        /// [5000; ) - Client errors
        /// </summary>
        public static class Client
        {
            public const uint UnknownError = 5000;
            public const uint AcquireLockError = 5001;
            public const uint ReleaseLockError = 5002;
            public const uint UpdateLockError = 5003;
        }
    }
}
