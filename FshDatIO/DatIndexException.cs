using System;
using System.Runtime.Serialization;

namespace FshDatIO
{
    /// <summary>
    /// The Exception thrown when a <see cref="DatIndex"/> does not exist within the <see cref="DatFile"/>
    /// </summary>
    [Serializable]
    public sealed class DatIndexException : DatFileException, ISerializable  
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DatIndexException"/> class.
        /// </summary>
        public DatIndexException() : base("A DatIndexException has occured")
        {}

        /// <summary>
        /// Initializes a new instance of the <see cref="DatIndexException"/> class with the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        public DatIndexException(string message) : base(message)
        {
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="DatIndexException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="inner">The inner exception.</param>
        public DatIndexException(string message, Exception inner) : base(message, inner)
        {
        }

        private DatIndexException(SerializationInfo info, StreamingContext context) : base(info, context)
        { 
        }
    }
}
