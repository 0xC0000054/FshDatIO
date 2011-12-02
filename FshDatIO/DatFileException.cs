using System;
using System.Runtime.Serialization;

namespace FshDatIO
{
    /// <summary>
    /// The base class for all other exceptions 
    /// </summary>
    [Serializable]
    public class DatFileException : Exception, ISerializable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DatFileException"/> class.
        /// </summary>
        public DatFileException() : base("A DatFileException has occured")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DatFileException"/> class the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        public DatFileException(string message) : base(message)
        { 
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DatFileException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="inner">The inner exception.</param>
        public DatFileException(string message, Exception inner) : base(message, inner)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DatFileException"/> class.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext"/> that contains contextual information about the source or destination.</param>
        /// <exception cref="T:System.ArgumentNullException">
        /// The <paramref name="info"/> parameter is null.
        ///   </exception>
        ///   
        /// <exception cref="T:System.Runtime.Serialization.SerializationException">
        /// The class name is null or <see cref="P:System.Exception.HResult"/> is zero (0).
        ///   </exception>
        protected DatFileException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

    }
}
