using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Lokman
{
    [ExcludeFromCodeCoverage]
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
}
