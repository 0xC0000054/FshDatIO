using System;

namespace FshDatIO
{
    static class QfsCompression
    {
        /// <summary>
        /// Decompresses a QFS compressed byte array.
        /// </summary>
        /// <param name="compressedData">The byte array to decompress</param>
        /// <returns>A byte array containing the decompressed data.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="compressedData"/> is null.</exception>
        /// <exception cref="System.NotSupportedException"><paramref name="compressedData"/> uses an unsupported compression format.</exception>
        public static byte[] Decompress(byte[] compressedData)
        {
            if (compressedData == null)
            {
                throw new ArgumentNullException("compressedData");
            }

            int startOffset = 0;

            if ((compressedData[0] & 0xfe) != 0x10 || compressedData[1] != 0xFB)
            {
                if (compressedData[4] != 0x10 || compressedData[5] != 0xFB)
                {
                    throw new NotSupportedException(FshDatIO.Properties.Resources.UnsupportedCompressionFormat);
                }
                startOffset = 4;
            }

            int outLength = ((compressedData[startOffset + 2] << 16) | (compressedData[startOffset + 3] << 8)) | compressedData[startOffset + 4];

            byte[] unCompressedData = new byte[outLength];

            int index = startOffset + 5;
            if (index == 5 && (compressedData[0] & 1) != 0)
            {
                // Some NFS files may need to be aligned to an 4 byte boundary.
                index = 8;
            }

            byte ccbyte1 = 0; // control char 0
            byte ccbyte2 = 0; // control char 1
            byte ccbyte3 = 0; // control char 2
            byte ccbyte4 = 0; // control char 3

            int outIndex = 0;
            int plainCount = 0;
            int copyCount = 0;
            int copyOffset = 0;

            int length = compressedData.Length;

            while (index < length && compressedData[index] < 0xFC)
            {
                ccbyte1 = compressedData[index];
                index++;

                if (ccbyte1 >= 0xE0) // 1 byte literal op code 0xE0 - 0xFB
                {
                    plainCount = ((ccbyte1 & 0x1F) << 2) + 4;
                    copyCount = 0;
                    copyOffset = 0;
                }
                else if (ccbyte1 >= 0xC0) // 4 byte op code 0xC0 - 0xDF
                {
                    ccbyte2 = compressedData[index];
                    index++;
                    ccbyte3 = compressedData[index];
                    index++;
                    ccbyte4 = compressedData[index];
                    index++;

                    plainCount = (ccbyte1 & 3);
                    copyCount = ((ccbyte1 & 0x0C) << 6) + ccbyte4 + 5;
                    copyOffset = (((ccbyte1 & 0x10) << 12) + (ccbyte2 << 8)) + ccbyte3 + 1;
                }
                else if (ccbyte1 >= 0x80) // 3 byte op code 0x80 - 0xBF
                {
                    ccbyte2 = compressedData[index];
                    index++;
                    ccbyte3 = compressedData[index];
                    index++;

                    plainCount = (ccbyte2 & 0xC0) >> 6;
                    copyCount = (ccbyte1 & 0x3F) + 4;
                    copyOffset = ((ccbyte2 & 0x3F) << 8) + ccbyte3 + 1;
                }
                else // 2 byte op code 0x00 - 0x7F
                {
                    ccbyte2 = compressedData[index];
                    index++;

                    plainCount = (ccbyte1 & 3);
                    copyCount = ((ccbyte1 & 0x1C) >> 2) + 3;
                    copyOffset = ((ccbyte1 & 0x60) << 3) + ccbyte2 + 1;
                }

                // Buffer.BlockCopy is faster than a for loop for data larger than 32 bytes.
                if (plainCount > 32)
                {
                    Buffer.BlockCopy(compressedData, index, unCompressedData, outIndex, plainCount);
                    index += plainCount;
                    outIndex += plainCount;
                }
                else
                {
                    for (int i = 0; i < plainCount; i++)
                    {
                        unCompressedData[outIndex] = compressedData[index];
                        index++;
                        outIndex++;
                    }
                }

                if (copyCount > 0)
                {
                    int srcIndex = outIndex - copyOffset;

                    if (copyCount > 32)
                    {
                        Buffer.BlockCopy(unCompressedData, srcIndex, unCompressedData, outIndex, copyCount);
                        outIndex += copyCount;
                    }
                    else
                    {
                        for (int i = 0; i < copyCount; i++)
                        {
                            unCompressedData[outIndex] = unCompressedData[srcIndex];
                            srcIndex++;
                            outIndex++;
                        }
                    }
                }
            }

            // Write the trailing bytes.
            if (index < length && outIndex < outLength)
            {
                // 1 byte EOF op code 0xFC - 0xFF.
                plainCount = (compressedData[index] & 3);
                index++;

                for (int i = 0; i < plainCount; i++)
                {
                    unCompressedData[outIndex] = compressedData[index];
                    index++;
                    outIndex++;
                }
            }

            return unCompressedData;
        }

        /// <summary>
        /// The minimum size in bytes of an uncompressed buffer that can be compressed with QFS compression.
        /// </summary>
        private const int UncompressedDataMinSize = 10;
        /// <summary>
        /// The maximum size in bytes of an uncompressed buffer that can be compressed with QFS compression.
        /// </summary>
        private const int UncompressedDataMaxSize = 16777215;

        /// <summary>
        /// Compresses the input byte array with QFS compression
        /// </summary>
        /// <param name="input">The input byte array to compress</param>
        /// <param name="prefixLength">If set to <c>true</c> prefix the size of the compressed data, as is used by SC4; otherwise <c>false</c>.</param>
        /// <returns>A byte array containing the compressed data or null if the data cannot be compressed.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="input" /> is null.</exception>
        /// <exception cref="System.FormatException">The length of <paramref name="input"/> is larger than 16777215 bytes.</exception>
        public static byte[] Compress(byte[] input, bool prefixLength)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }

            if (input.Length > UncompressedDataMaxSize)
            {
                throw new FormatException(FshDatIO.Properties.Resources.UncompressedBufferTooLarge);
            }

            if (input.Length < UncompressedDataMinSize)
            {
                return null;
            }

            ZlibQFS qfs = new ZlibQFS(input, prefixLength);
            return qfs.Compress();
        }

        private sealed class ZlibQFS
        {
            private byte[] input;
            private byte[] output;
            private int inputLength;
            private int outputLength;
            private int outIndex;
            private int readPosition;
            private int lastWritePosition;
            private int remaining;
            private bool prefixLength;

            private const int QfsHeaderSize = 5;
            /// <summary>
            /// The maximum length of a literal run.
            /// </summary>
            private const int LiteralRunMaxLength = 112;

            private int hash;
            private int[] head;
            private int[] prev;

            private const int MaxWindowSize = 131072;
            private const int MaxHashSize = 65536;

            private readonly int WindowSize;
            private readonly int WindowMask;
            private readonly int MaxWindowOffset;

            private readonly int HashSize;
            private readonly int HashMask;
            private readonly int HashShift;

            private const int GoodLength = 32;
            private const int MaxLazy = 258;
            private const int NiceLength = 258;
            private const int MaxChain = 4096;
            private const int MIN_MATCH = 3;
            private const int MAX_MATCH = 1028;

            private int match_start;
            private int match_length;
            private int prev_length;

            private static int HighestOneBit(int value)
            {
                value--;
                value |= (value >> 1);
                value |= (value >> 2);
                value |= (value >> 4);
                value |= (value >> 8);
                value |= (value >> 16);
                value++;

                return value - (value >> 1);
            }

            private static int NumberOfTrailingZeros(int value)
            {
                uint v = (uint)value; // 32-bit word input to count zero bits on right
                int count;            // count will be the number of zero bits on the right,
                // so if v is 1101000 (base 2), then count will be 3

                if (v == 0)
                {
                    return 32;
                }

                if ((v & 0x1) != 0)
                {
                    // special case for odd v (assumed to happen half of the time)
                    count = 0;
                }
                else
                {
                    count = 1;
                    if ((v & 0xffff) == 0)
                    {
                        v >>= 16;
                        count += 16;
                    }
                    if ((v & 0xff) == 0)
                    {
                        v >>= 8;
                        count += 8;
                    }
                    if ((v & 0xf) == 0)
                    {
                        v >>= 4;
                        count += 4;
                    }
                    if ((v & 0x3) == 0)
                    {
                        v >>= 2;
                        count += 2;
                    }
                    if ((v & 0x1) != 0)
                    {
                        count--;
                    }
                }

                return count;
            }

            public ZlibQFS(byte[] input, bool prefixLength)
            {
                if (input == null)
                {
                    throw new ArgumentNullException("input");
                }

                this.input = input;
                this.inputLength = input.Length;
                this.output = new byte[this.inputLength - 1];
                this.outputLength = output.Length;

                if (this.inputLength < MaxWindowSize)
                {
                    WindowSize = HighestOneBit(this.inputLength);
                    HashSize = Math.Max(WindowSize / 2, 32);
                    HashShift = (NumberOfTrailingZeros(HashSize) + MIN_MATCH - 1) / MIN_MATCH;
                }
                else
                {
                    WindowSize = MaxWindowSize;
                    HashSize = MaxHashSize;
                    HashShift = 6;
                }
                MaxWindowOffset = WindowSize - 1;
                WindowMask = MaxWindowOffset;
                HashMask = HashSize - 1;

                this.hash = 0;
                this.head = new int[HashSize];
                this.prev = new int[WindowSize];
                this.readPosition = 0;
                this.remaining = inputLength;
                this.outIndex = QfsHeaderSize;
                this.lastWritePosition = 0;
                this.prefixLength = prefixLength;
            }

            private bool WriteCompressedData(int startOffset)
            {
                int endOffset = this.readPosition - 1;
                int run = endOffset - this.lastWritePosition;

                while (run > 3) // 1 byte literal op code 0xE0 - 0xFB
                {
                    int blockLength = Math.Min(run & ~3, LiteralRunMaxLength);

                    if ((this.outIndex + blockLength + 1) >= this.outputLength)
                    {
                        return false; // data did not compress
                    }

                    this.output[this.outIndex] = (byte)(0xE0 + ((blockLength / 4) - 1));
                    this.outIndex++;

                    // A for loop is faster than Buffer.BlockCopy for data less than or equal to 32 bytes.
                    if (blockLength <= 32)
                    {
                        for (int i = 0; i < blockLength; i++)
                        {
                            this.output[this.outIndex] = this.input[this.lastWritePosition];
                            this.lastWritePosition++;
                            this.outIndex++;
                        }
                    }
                    else
                    {
                        Buffer.BlockCopy(this.input, this.lastWritePosition, this.output, this.outIndex, blockLength);
                        this.lastWritePosition += blockLength;
                        this.outIndex += blockLength;
                    }

                    run -= blockLength;
                }

                int copyLength = this.prev_length;
                int copyOffset = endOffset - startOffset - 1;

                if (copyLength <= 10 && copyOffset < 1024) // 2 byte op code  0x00 - 0x7f
                {
                    if ((this.outIndex + run + 2) >= this.outputLength)
                    {
                        return false;
                    }

                    this.output[this.outIndex] = (byte)((((copyOffset >> 8) << 5) + ((copyLength - 3) << 2)) + run);
                    this.output[this.outIndex + 1] = (byte)(copyOffset & 0xff);
                    this.outIndex += 2;
                }
                else if (copyLength <= 67 && copyOffset < 16384)  // 3 byte op code 0x80 - 0xBF
                {
                    if ((this.outIndex + run + 3) >= this.outputLength)
                    {
                        return false;
                    }

                    this.output[this.outIndex] = (byte)(0x80 + (copyLength - 4));
                    this.output[this.outIndex + 1] = (byte)((run << 6) + (copyOffset >> 8));
                    this.output[this.outIndex + 2] = (byte)(copyOffset & 0xff);
                    this.outIndex += 3;
                }
                else // 4 byte op code 0xC0 - 0xDF
                {
                    if ((this.outIndex + run + 4) >= this.outputLength)
                    {
                        return false;
                    }

                    this.output[this.outIndex] = (byte)(((0xC0 + ((copyOffset >> 16) << 4)) + (((copyLength - 5) >> 8) << 2)) + run);
                    this.output[this.outIndex + 1] = (byte)((copyOffset >> 8) & 0xff);
                    this.output[this.outIndex + 2] = (byte)(copyOffset & 0xff);
                    this.output[this.outIndex + 3] = (byte)((copyLength - 5) & 0xff);
                    this.outIndex += 4;
                }


                for (int i = 0; i < run; i++)
                {
                    this.output[this.outIndex] = this.input[this.lastWritePosition];
                    this.lastWritePosition++;
                    this.outIndex++;
                }
                this.lastWritePosition += copyLength;

                return true;
            }

            private bool WriteEndData()
            {
                int run = this.readPosition - this.lastWritePosition;

                while (run > 3) // 1 byte literal op code 0xE0 - 0xFB
                {
                    int blockLength = Math.Min(run & ~3, LiteralRunMaxLength);

                    if ((this.outIndex + blockLength + 1) >= this.outputLength)
                    {
                        return false; // data did not compress
                    }

                    this.output[this.outIndex] = (byte)(0xE0 + ((blockLength / 4) - 1));
                    this.outIndex++;

                    // A for loop is faster than Buffer.BlockCopy for data less than or equal to 32 bytes.
                    if (blockLength <= 32)
                    {
                        for (int i = 0; i < blockLength; i++)
                        {
                            this.output[this.outIndex] = this.input[this.lastWritePosition];
                            this.lastWritePosition++;
                            this.outIndex++;
                        }
                    }
                    else
                    {
                        Buffer.BlockCopy(this.input, this.lastWritePosition, this.output, this.outIndex, blockLength);
                        this.lastWritePosition += blockLength;
                        this.outIndex += blockLength;
                    }
                    run -= blockLength;
                }

                if ((this.outIndex + run + 1) >= this.outputLength)
                {
                    return false;
                }
                this.output[this.outIndex] = (byte)(0xFC + run);
                this.outIndex++;

                for (int i = 0; i < run; i++)
                {
                    this.output[this.outIndex] = this.input[this.lastWritePosition];
                    this.lastWritePosition++;
                    this.outIndex++;
                }

                return true;
            }

            // longest_match and Compress are adapted from deflate.c in zlib 1.2.3 which is licensed as follows:
            /* zlib.h -- interface of the 'zlib' general purpose compression library
              version 1.2.3, July 18th, 2005

              Copyright (C) 1995-2005 Jean-loup Gailly and Mark Adler

              This software is provided 'as-is', without any express or implied
              warranty.  In no event will the authors be held liable for any damages
              arising from the use of this software.

              Permission is granted to anyone to use this software for any purpose,
              including commercial applications, and to alter it and redistribute it
              freely, subject to the following restrictions:

              1. The origin of this software must not be misrepresented; you must not
                 claim that you wrote the original software. If you use this software
                 in a product, an acknowledgment in the product documentation would be
                 appreciated but is not required.
              2. Altered source versions must be plainly marked as such, and must not be
                 misrepresented as being the original software.
              3. This notice may not be removed or altered from any source distribution.

              Jean-loup Gailly        Mark Adler
              jloup@gzip.org          madler@alumni.caltech.edu


              The data format used by the zlib library is described by RFCs (Request for
              Comments) 1950 to 1952 in the files http://www.ietf.org/rfc/rfc1950.txt
              (zlib format), rfc1951.txt (deflate format) and rfc1952.txt (gzip format).
            */

            private int longest_match(int cur_match)
            {
                int chain_length = MaxChain;
                int scan = this.readPosition;
                int bestLength = this.prev_length;

                if (bestLength >= this.remaining)
                {
                    return remaining;
                }

                byte scan_end1 = input[scan + bestLength - 1];
                byte scan_end = input[scan + bestLength];

                // Do not waste too much time if we already have a good match:
                if (this.prev_length >= GoodLength)
                {
                    chain_length >>= 2;
                }
                int niceLength = NiceLength;

                // Do not look for matches beyond the end of the input. This is necessary
                // to make deflate deterministic.
                if (niceLength > this.remaining)
                {
                    niceLength = this.remaining;
                }
                int maxLength = Math.Min(this.remaining, MAX_MATCH);
                int limit = this.readPosition > MaxWindowOffset ? this.readPosition - MaxWindowOffset : 0;

                do
                {
                    int match = cur_match;

                    // Skip to next match if the match length cannot increase
                    // or if the match length is less than 2:
                    if (input[match + bestLength] != scan_end ||
                        input[match + bestLength - 1] != scan_end1 ||
                        input[match] != input[scan] ||
                        input[match + 1] != input[scan + 1])
                    {
                        continue;
                    }


                    int len = 2;
                    do
                    {
                        len++;
                    }
                    while (len < maxLength && input[scan + len] == input[match + len]);

                    if (len > bestLength)
                    {
                        this.match_start = cur_match;
                        bestLength = len;
                        if (len >= niceLength)
                        {
                            break;
                        }
                        scan_end1 = input[scan + bestLength - 1];
                        scan_end = input[scan + bestLength];
                    }
                }
                while ((cur_match = prev[cur_match & WindowMask]) >= limit && --chain_length > 0);

                return bestLength;
            }

            public byte[] Compress()
            {
                for (int i = 0; i < head.Length; i++)
                {
                    head[i] = -1;
                }

                this.hash = input[0];
                this.hash = ((this.hash << HashShift) ^ input[1]) & HashMask;

                int lastMatch = this.inputLength - MIN_MATCH;

                while (remaining > 0)
                {
                    this.prev_length = this.match_length;
                    int prev_match = this.match_start;
                    this.match_length = MIN_MATCH - 1;

                    int hash_head = -1;

                    // Insert the string window[readPosition .. readPosition+2] in the
                    // dictionary, and set hash_head to the head of the hash chain:
                    if (this.remaining >= MIN_MATCH)
                    {
                        this.hash = ((this.hash << HashShift) ^ input[this.readPosition + MIN_MATCH - 1]) & HashMask;

                        hash_head = head[this.hash];
                        prev[this.readPosition & WindowMask] = hash_head;
                        head[this.hash] = this.readPosition;
                    }

                    if (hash_head >= 0 && this.prev_length < MaxLazy && this.readPosition - hash_head <= WindowSize)
                    {
                        int bestLength = longest_match(hash_head);

                        if (bestLength >= MIN_MATCH)
                        {
                            int bestOffset = this.readPosition - this.match_start;

                            if (bestOffset <= 1024 ||
                                bestOffset <= 16384 && bestLength >= 4 ||
                                bestOffset <= WindowSize && bestLength >= 5)
                            {
                                this.match_length = bestLength;
                            }
                        }
                    }

                    // If there was a match at the previous step and the current
                    // match is not better, output the previous match:
                    if (this.prev_length >= MIN_MATCH && this.match_length <= this.prev_length)
                    {
                        if (!WriteCompressedData(prev_match))
                        {
                            return null;
                        }

                        // Insert in hash table all strings up to the end of the match.
                        // readPosition-1 and readPosition are already inserted. If there is not
                        // enough lookahead, the last two strings are not inserted in
                        // the hash table.

                        this.remaining -= (this.prev_length - 1);
                        this.prev_length -= 2;

                        do
                        {
                            this.readPosition++;

                            if (this.readPosition < lastMatch)
                            {
                                this.hash = ((this.hash << HashShift) ^ input[this.readPosition + MIN_MATCH - 1]) & HashMask;

                                hash_head = head[this.hash];
                                prev[this.readPosition & WindowMask] = hash_head;
                                head[this.hash] = this.readPosition;
                            }
                            this.prev_length--;
                        }
                        while (prev_length > 0);

                        this.match_length = MIN_MATCH - 1;
                        this.readPosition++;
                    }
                    else
                    {
                        this.readPosition++;
                        this.remaining--;
                    }
                }

                if (!WriteEndData())
                {
                    return null;
                }

                // Write the compressed data header.
                output[0] = 0x10;
                output[1] = 0xFB;
                output[2] = (byte)((inputLength >> 16) & 0xff);
                output[3] = (byte)((inputLength >> 8) & 0xff);
                output[4] = (byte)(inputLength & 0xff);

                // Trim the output array to its actual size.
                if (prefixLength)
                {
                    int finalLength = outIndex + 4;
                    if (finalLength >= inputLength)
                    {
                        return null;
                    }

                    byte[] temp = new byte[finalLength];
                    // Write the compressed data length in little endian byte order.
                    temp[0] = (byte)(outIndex & 0xff);
                    temp[1] = (byte)((outIndex >> 8) & 0xff);
                    temp[2] = (byte)((outIndex >> 16) & 0xff);
                    temp[3] = (byte)((outIndex >> 24) & 0xff);

                    Buffer.BlockCopy(this.output, 0, temp, 4, outIndex);
                    this.output = temp;
                }
                else
                {
                    byte[] temp = new byte[outIndex];
                    Buffer.BlockCopy(this.output, 0, temp, 0, outIndex);

                    this.output = temp;
                }

                return output;
            }

        }
    }
}
