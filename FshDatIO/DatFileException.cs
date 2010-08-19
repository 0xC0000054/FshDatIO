using System;
using System.Collections.Generic;
using System.Text;

namespace FshDatIO
{
    class DatFileException : Exception
    {
        public DatFileException() : base("A DatFileException has occured")
        {

        }
        public DatFileException(string message) : base(message)
        { 
        
        }
    }
}
