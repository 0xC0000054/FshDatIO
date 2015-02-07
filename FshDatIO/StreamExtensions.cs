using System.IO;

namespace FshDatIO
{
    static class StreamExtensions
    {
        private const int DefaultBufferSize = 4;
        private static byte[] buffer;

        private static void FillBuffer(Stream s, int count)
        {
            if (buffer == null)
            {
                buffer = new byte[DefaultBufferSize];
            }

            if (count > buffer.Length)
            {
                throw new System.ArgumentOutOfRangeException("count", "count > buffer.Length");
            }

            int bytesRead = 0;
            int n;

            do
            {
                n = s.Read(buffer, bytesRead, count - bytesRead);
                if (n == 0)
                {
                    throw new EndOfStreamException();
                }
                bytesRead += n;

            } while (bytesRead < count);
        }

        public static ushort ReadUInt16(this Stream s)
        {
            FillBuffer(s, 2);

            return (ushort)(buffer[0] | (buffer[1] << 8));
        }

        public static uint ReadUInt32(this Stream s)
        {
            FillBuffer(s, 4);

            return (uint)(buffer[0] | (buffer[1] << 8) | (buffer[2] << 16) | (buffer[3] << 24));
        }

        public static int ReadInt32(this Stream s)
        {
            FillBuffer(s, 4);

            return (int)(buffer[0] | (buffer[1] << 8) | (buffer[2] << 16) | (buffer[3] << 24));
        }

        public static byte[] ReadBytes(this Stream s, int count)
        {
            byte[] buffer = new byte[count];

            int totalBytesRead = count;
            int offset = 0;

            while (totalBytesRead > 0)
            {
                // Read may return anything from 0 to numBytesToRead.
                int n = s.Read(buffer, offset, totalBytesRead);
                // The end of the file is reached.
                if (n == 0)
                {
                    throw new EndOfStreamException();
                }

                offset += n;
                totalBytesRead -= n;
            }

            return buffer;
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
