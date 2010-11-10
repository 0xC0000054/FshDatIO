using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;

namespace FshDatIO
{
    [Serializable]
    public class DatFileException : Exception, ISerializable
    {
        public DatFileException() : base("A DatFileException has occured")
        {
        }

        public DatFileException(string message) : base(message)
        { 
        }

        public DatFileException(string message, Exception inner) : base(message, inner)
        {
        }

        protected DatFileException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

    }
}
