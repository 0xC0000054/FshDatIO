using System.IO;

namespace FshDatIO
{
    static class StreamExtensions
    {
        public static void ProperRead(this Stream s, byte[] buffer, int offset, int count)
        {
            int numBytesToRead = count;
            int numBytesRead = 0 + offset;

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

        public static void WriteUInt16(this Stream s, ushort value)
        {
            s.WriteByte((byte)(value & 0xff));
            s.WriteByte((byte)((value >> 8) & 0xff));
        }

        public static void WriteUInt32(this Stream s, uint value)
        {
            s.WriteByte((byte)(value & 0xff));
            s.WriteByte((byte)((value >> 8) & 0xff));
            s.WriteByte((byte)((value >> 16) & 0xff));
            s.WriteByte((byte)((value >> 24) & 0xff));
        }

        public static void WriteInt32(this Stream s, int value)
        {
            s.WriteByte((byte)(value & 0xff));
            s.WriteByte((byte)((value >> 8) & 0xff));
            s.WriteByte((byte)((value >> 16) & 0xff));
            s.WriteByte((byte)((value >> 24) & 0xff));
        }
    }
}
