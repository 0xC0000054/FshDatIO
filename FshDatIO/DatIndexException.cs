using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;

namespace FshDatIO
{
    [Serializable]
    public sealed class DatIndexException : DatFileException, ISerializable  
    {
        public DatIndexException() : base("A DatIndexException has occured")
        {}

        public DatIndexException(string message) : base(message)
        {
        }
        public DatIndexException(string message, Exception inner) : base(message, inner)
        {
        }

        private DatIndexException(SerializationInfo info, StreamingContext context) : base(info, context)
        { 
        }
    }
}
