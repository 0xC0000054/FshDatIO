using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;

namespace FshDatIO
{
    [Serializable]
    public sealed class DatHeaderException : DatFileException, ISerializable
    {
        public DatHeaderException() : base("A DatHeaderException has occured")
        {
        }

        public DatHeaderException(string message) : base(message)
        {
        }
        public DatHeaderException(string message, Exception inner) : base(message, inner)
        {
        }

        private DatHeaderException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
