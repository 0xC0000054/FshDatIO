using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace FshDatIO
{
    static class StreamExtensions
    {
        /// <summary>
        /// Reads a byte from the Stream and advances the read position by one byte 
        /// </summary>
        /// <returns>The byte read or throws an EndOfStreamException if the stream end has been reached</returns>
        /// <exception cref="System.IO.EndOfStreamException">The end of the stream is reached.</exception>
        public static byte ReadByte2(this Stream s)
        {
            int val = s.ReadByte();

            if (val == -1)
            {
                throw new EndOfStreamException();
            }

            return (byte)val;
        }

        public static void ProperRead(this Stream s, byte[] buffer, int offset, int count)
        {
            s.Seek((long)offset, SeekOrigin.Begin);

            int numBytesToRead = count;
            int numBytesRead = 0;
            while (numBytesToRead > 0)
            {
                // Read may return anything from 0 to numBytesToRead.
                int n = s.Read(buffer, numBytesRead, numBytesToRead);
                // The end of the file is reached.
                if (n == 0)
                    break;
                numBytesRead += n;
                numBytesToRead -= n;
            }
        }

    }
}
