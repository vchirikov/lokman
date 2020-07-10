using System;
using System.Runtime.Serialization;

namespace Lokman
{
    /// <inheritdoc cref="Exception"/>
    [Serializable]
    public class LokmanException : Exception
    {
        /// <inheritdoc cref="Exception()"/>
        public LokmanException() { }

        /// <inheritdoc cref="Exception(string?)"/>
        public LokmanException(string message) : base(message) { }

        /// <inheritdoc cref="Exception(string?, Exception?)"/>
        public LokmanException(string message, Exception innerException) : base(message, innerException) { }

        /// <inheritdoc cref="Exception(SerializationInfo, StreamingContext)"/>
        protected LokmanException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}