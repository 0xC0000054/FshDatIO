using System;
using System.Runtime.Serialization;

namespace FshDatIO
{
    /// <summary>
    /// The exception thrown when the <see cref="DatFile"/>'s Header is invalid.
    /// </summary>
    [Serializable]
    public sealed class DatHeaderException : DatFileException, ISerializable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DatHeaderException"/> class.
        /// </summary>
        public DatHeaderException() : base("A DatHeaderException has occured")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DatHeaderException"/> class with the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        public DatHeaderException(string message) : base(message)
        {
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="DatHeaderException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="inner">The inner exception.</param>
        public DatHeaderException(string message, Exception inner) : base(message, inner)
        {
        }

        private DatHeaderException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
