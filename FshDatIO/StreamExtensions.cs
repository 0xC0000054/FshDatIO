﻿using System.IO;

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

        /// <summary>
        /// Reads a 4-byte unsigned integer from the stream in little endian byte order and advances the position of the stream by four bytes.
        /// </summary>
        /// <param name="s">The stream.</param>
        /// <exception cref="System.IO.EndOfStreamException">The end of the stream is reached.</exception>
        /// <returns>A 4-byte unsigned integer read from the current stream.</returns>
        public static uint ReadUInt32(this Stream s)
        {
            FillBuffer(s, 4);

            return (uint)(buffer[0] | (buffer[1] << 8) | (buffer[2] << 16) | (buffer[3] << 24));
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

        /// <summary>
        /// Writes an unsigned 16-bit integer to the stream in little endian byte order.
        /// </summary>
        /// <param name="s">The stream.</param>
        /// <param name="value">The value.</param>
        public static void WriteUInt16(this Stream s, ushort value)
        {
            s.WriteByte((byte)(value & 0xff));
            s.WriteByte((byte)((value >> 8) & 0xff));
        }

        /// <summary>
        /// Writes an unsigned 32-bit integer to the stream in little endian byte order.
        /// </summary>
        /// <param name="s">The stream.</param>
        /// <param name="value">The value.</param>
        public static void WriteUInt32(this Stream s, uint value)
        {
            s.WriteByte((byte)(value & 0xff));
            s.WriteByte((byte)((value >> 8) & 0xff));
            s.WriteByte((byte)((value >> 16) & 0xff));
            s.WriteByte((byte)((value >> 24) & 0xff));
        }
        
        /// <summary>
        /// Writes an signed 32-bit integer to the stream in little endian byte order.
        /// </summary>
        /// <param name="s">The stream.</param>
        /// <param name="value">The value.</param>
        public static void WriteInt32(this Stream s, int value)
        {
            s.WriteByte((byte)(value & 0xff));
            s.WriteByte((byte)((value >> 8) & 0xff));
            s.WriteByte((byte)((value >> 16) & 0xff));
            s.WriteByte((byte)((value >> 24) & 0xff));
        }
    }
}
