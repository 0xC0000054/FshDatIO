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

using System;

namespace FshDatIO
{
    /// <summary>
    /// Converts an array of bytes to base data types in little endian byte order.
    /// </summary>
    internal static class LittleEndianBitConverter
    {
        private static readonly bool IsLittleEndian = BitConverter.IsLittleEndian;

        /// <summary>
        /// Returns a 16-bit unsigned integer converted from two bytes at a specified position in a byte array.
        /// </summary>
        /// <param name="value">An array of bytes.</param>
        /// <param name="startIndex">The starting position within <paramref name="value"/>.</param>
        /// <returns>A 16-bit unsigned integer formed by two bytes beginning at <paramref name="startIndex"/>.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="value"/> is null</exception>
        /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="startIndex"/> is less than zero or greater than the length of <paramref name="value"/> minus 1.</exception>
        public static unsafe ushort ToUInt16(byte[] value, int startIndex)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            if (startIndex < 0 || startIndex > (value.Length - 2))
            {
                throw new ArgumentOutOfRangeException("startIndex");
            }

            fixed (byte* ptr = &value[startIndex])
            {
                if (IsLittleEndian && (startIndex % 2) == 0)
                {
                    // If we are aligned cast the pointer directly.
                    return *((ushort*)ptr);
                }
                else
                {
                    return (ushort)(*ptr | (*(ptr + 1) << 8));
                }
            }
        }

        /// <summary>
        /// Returns a 32-bit signed integer converted from four bytes at a specified position in a byte array.
        /// </summary>
        /// <param name="value">An array of bytes.</param>
        /// <param name="startIndex">The starting position within <paramref name="value"/>.</param>
        /// <returns>A 32-bit signed integer formed by four bytes beginning at <paramref name="startIndex"/>.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="value"/> is null</exception>
        /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="startIndex"/> is less than zero or greater than the length of <paramref name="value"/> minus 1.</exception>
        public static unsafe int ToInt32(byte[] value, int startIndex)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            if (startIndex < 0 || startIndex > (value.Length - 4))
            {
                throw new ArgumentOutOfRangeException("startIndex");
            }

            fixed (byte* ptr = &value[startIndex])
            {
                if (IsLittleEndian && (startIndex % 4) == 0)
                {
                    // If we are aligned cast the pointer directly.
                    return *((int*)ptr);
                }
                else
                {
                    return *ptr | (*(ptr + 1) << 8) | (*(ptr + 2) << 16) | (*(ptr + 3) << 24);
                }
            }
        }

        /// <summary>
        /// Returns a 32-bit unsigned integer converted from four bytes at a specified position in a byte array.
        /// </summary>
        /// <param name="value">An array of bytes.</param>
        /// <param name="startIndex">The starting position within <paramref name="value"/>.</param>
        /// <returns>A 32-bit unsigned integer formed by four bytes beginning at <paramref name="startIndex"/>.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="value"/> is null</exception>
        /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="startIndex"/> is less than zero or greater than the length of <paramref name="value"/> minus 1.</exception>
        public static unsafe uint ToUInt32(byte[] value, int startIndex)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            if (startIndex < 0 || startIndex > (value.Length - 4))
            {
                throw new ArgumentOutOfRangeException("startIndex");
            }

            return (uint)ToInt32(value, startIndex);
        }
    }
}
