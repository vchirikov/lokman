using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace Lokman
{
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    public readonly struct Epoch : IComparable<Epoch>, IEquatable<Epoch>, IComparable, ISerializable
    {
        /// <summary>
        /// Increasing counter
        /// </summary>
        public readonly long Index;

        /// <summary>
        /// Internal system timestamp (!= client timestamp)
        /// </summary>
        public readonly long Ticks;

        public Epoch(long index, long ticks) => (Index, Ticks) = (index, ticks);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public Epoch(SerializationInfo info, StreamingContext context)
            => (Index, Ticks) = (info.GetInt64(nameof(Index)), info.GetInt64(nameof(Ticks)));

        /// <inheritdoc />
        public int CompareTo(Epoch other)
        {
            var idxComparison = Index - other.Index;
            if (idxComparison != 0)
                return unchecked((int)idxComparison);
            return unchecked((int)(Ticks - other.Ticks));
        }

        /// <inheritdoc />
        public int CompareTo(object? obj)
        {
            if (obj is null)
                return 1;

            if (!(obj is Epoch epoch))
                throw new ArgumentException($"Object must be of type {nameof(Epoch)}");

            return CompareTo(epoch);
        }

        /// <inheritdoc />
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Index), Index);
            info.AddValue(nameof(Ticks), Ticks);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is Epoch epoch && Equals(epoch);

        /// <inheritdoc />
        public bool Equals(Epoch other) => (other.Index, other.Ticks) == (Index, Ticks);

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(Index, Ticks);

        /// <inheritdoc cref="long.ToString" />
        public override string ToString() => (this == default) ? "" : "["
            + new DateTime(Ticks).ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)
            + "] "
            + Index.ToString(CultureInfo.InvariantCulture);


        public static bool operator ==(Epoch left, Epoch right) => left.Equals(right);

        public static bool operator !=(Epoch left, Epoch right) => !left.Equals(right);

        public static bool operator <(Epoch left, Epoch right) => left.CompareTo(right) < 0;

        public static bool operator >(Epoch left, Epoch right) => left.CompareTo(right) > 0;

        public static bool operator <=(Epoch left, Epoch right) => left.CompareTo(right) <= 0;

        public static bool operator >=(Epoch left, Epoch right) => left.CompareTo(right) >= 0;
    }
}
