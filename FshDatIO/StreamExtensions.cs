/*
*  This file is part of FshDatIO, a library that manipulates SC4
*  DBPF files and FSH images.
*
*  Copyright (C) 2010-2017, 2023 Nicholas Hayes
*
*  This program is free software: you can redistribute it and/or modify
*  it under the terms of the GNU General Public License as published by
*  the Free Software Foundation, either version 3 of the License, or
*  (at your option) any later version.
*
*  This program is distributed in the hope that it will be useful,
*  but WITHOUT ANY WARRANTY; without even the implied warranty of
*  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
*  GNU General Public License for more details.
*
*  You should have received a copy of the GNU General Public License
*  along with this program.  If not, see <http://www.gnu.org/licenses/>.
*
*/

using System.IO;

namespace FshDatIO
{
    static class StreamExtensions
    {
        /// <summary>
        /// Reads a 4-byte unsigned integer from the stream in little endian byte order and advances the position of the stream by four bytes.
        /// </summary>
        /// <param name="s">The stream.</param>
        /// <exception cref="EndOfStreamException">The end of the stream is reached.</exception>
        /// <returns>A 4-byte unsigned integer read from the current stream.</returns>
        public static uint ReadUInt32(this Stream s)
        {
            int byte1 = s.ReadByte();
            if (byte1 == -1)
            {
                throw new EndOfStreamException();
            }

            int byte2 = s.ReadByte();
            if (byte2 == -1)
            {
                throw new EndOfStreamException();
            }

            int byte3 = s.ReadByte();
            if (byte3 == -1)
            {
                throw new EndOfStreamException();
            }

            int byte4 = s.ReadByte();
            if (byte4 == -1)
            {
                throw new EndOfStreamException();
            }

            return (uint)(byte1 | (byte2 << 8) | (byte3 << 16) | (byte4 << 24));
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
