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

            if (startIndex < 0 || startIndex > (value.Length - 4))
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
        /// Returns a 32-bit signed integer converted from two bytes at a specified position in a byte array.
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
                    return (int)(*ptr | (*(ptr + 1) << 8) | (*(ptr + 2) << 16) | (*(ptr + 3) << 24));
                }
            }
        }

        /// <summary>
        /// Returns a 32-bit unsigned integer converted from two bytes at a specified position in a byte array.
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

            fixed (byte* ptr = &value[startIndex])
            {
                if (IsLittleEndian && (startIndex % 4) == 0)
                {
                    // If we are aligned cast the pointer directly.
                    return *((uint*)ptr);
                }
                else
                {
                    return (uint)(*ptr | (*(ptr + 1) << 8) | (*(ptr + 2) << 16) | (*(ptr + 3) << 24));
                }
            }
        }
    }
}
