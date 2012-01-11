using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace FshDatIO
{
    static class StreamExtensions
    {
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
