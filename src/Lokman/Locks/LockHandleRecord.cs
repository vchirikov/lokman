using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Lokman
{
    public readonly struct LockHandleRecord : IEquatable<LockHandleRecord>
    {
        public readonly string Key;
        public readonly long Token;

        public LockHandleRecord(string key, long token) => (Key, Token) = (key, token);
        public override int GetHashCode() => HashCode.Combine(Key, Token);
        public void Deconstruct(out string key, out long token) => (key, token) = (Key, Token);
        public override bool Equals(object? obj) => obj is LockHandleRecord other && string.Equals(Key, other.Key, StringComparison.OrdinalIgnoreCase) && Token == other.Token;
        public bool Equals([AllowNull] LockHandleRecord other) => string.Equals(Key, other.Key, StringComparison.OrdinalIgnoreCase) && Token == other.Token;
        public static implicit operator (string Key, long Token)(LockHandleRecord value) => (value.Key, value.Token);
        public static implicit operator LockHandleRecord((string Key, long Token) value) => new LockHandleRecord(value.Key, value.Token);
        public override string ToString() => $"Key: \"{Key ?? "null"}\" Token: {Token.ToString(CultureInfo.InvariantCulture)}";
    }
}
