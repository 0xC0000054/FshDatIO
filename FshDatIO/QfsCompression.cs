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
        /// The number of iterations to use when searching for matches.
        /// </summary>
        private const int QfsMaxIterCount = 150;
        /// <summary>
        /// The maximum size of a compressed block.
        /// </summary>
        private const int QfsMaxBlockSize = 1028;
        /// <summary>
        /// The maximum length of the LZSS sliding window.
        /// </summary>
        private const int MaxWindowLength = 131072;
        /// <summary>
        /// The maximum length of a literal run.
        /// </summary>
        private const int LiteralRunMaxLength = 112;
        /// <summary>
        /// The maximum size in bytes of an uncompressed buffer that can be compressed with QFS compression.
        /// </summary>
        private const int UncompressedDataMaxSize = 16777215;
        /// <summary>
        /// The minimum match length.
        /// </summary>
        private const int MIN_MATCH = 3;

        /// <summary>
        /// Calculates the next highest the power of two.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The next highest power of 2.</returns>
        private static int NextPowerOfTwo(int value)
        {
            value |= (value >> 1);
            value |= (value >> 2);
            value |= (value >> 4);
            value |= (value >> 8);
            value |= (value >> 16);
            value++;

            return value;
        }

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
            int inputLength = input.Length;

            // If the input is smaller than MaxWindowLength use the next highest power of 2 as the sliding window length to save memory.
            int windowLength = inputLength < MaxWindowLength ? NextPowerOfTwo(inputLength) : MaxWindowLength;
            int windowMask = windowLength - 1;
            
            int[] similar_rev = new int[windowLength];
            int[,] last_rev = new int[256, 256];

            for (int i = 0; i < similar_rev.Length; i++)
            {
                similar_rev[i] = -1;
            }

            for (int i = 0; i < 256; i++)
            {
                for (int j = 0; j < 256; j++)
                {
                    last_rev[i, j] = -1;
                }
            }
            
            int outLength = inputLength - 1;
            byte[] outbuf = new byte[outLength];
            outbuf[0] = 0x10;
            outbuf[1] = 0xFB;
            outbuf[2] = (byte)((inputLength >> 16) & 0xff);
            outbuf[3] = (byte)((inputLength >> 8) & 0xff);
            outbuf[4] = (byte)(inputLength & 0xff);
            int outIndex = 5;

            int run = 0;
            int lastwrot = 0;
            int index = 0;

            int remaining = inputLength;

            while (remaining > 0)
            {
                int offs = -1;

                if (remaining > 2)
                {
                    offs = last_rev[input[index], input[index + 1]];
                    similar_rev[index & windowMask] = offs;
                    last_rev[input[index], input[index + 1]] = index;
                }

                if (index >= lastwrot)
                {
                    int bestLength = 0;
                    int bestOffset = 0;
                    int iterCount = 0;
                    int maxRun = Math.Min(remaining, QfsMaxBlockSize);

                    while (offs >= 0 && (index - offs) < windowLength && iterCount < QfsMaxIterCount)
                    {
                        run = MIN_MATCH - 1;
                        while (run < maxRun && input[index + run] == input[offs + run])
                        {
                            run++;
                        }

                        if (run > bestLength && run >= MIN_MATCH)
                        {
                            int offset = index - offs;

                            if (offset <= 1024 ||
                                offset <= 16384 && run >= 4 ||
                                offset <= windowLength && run >= 5)
                            {
                                bestLength = run;
                                bestOffset = offset - 1;
                            }
                        }
                        offs = similar_rev[offs & windowMask];
                        iterCount++;
                    }

                    if (bestLength > 0)
                    {
                        run = index - lastwrot;
                        while (run > 3) // 1 byte literal op code 0xE0 - 0xFB
                        {
                            int blockLength = Math.Min(run & ~3, LiteralRunMaxLength);
                            if ((outIndex + blockLength + 1) >= outLength)
                            {
                                return null; // data did not compress so return null
                            }

                            outbuf[outIndex] = (byte)(0xE0 + ((blockLength / 4) - 1));
                            outIndex++;

                            Buffer.BlockCopy(input, lastwrot, outbuf, outIndex, blockLength);
                            lastwrot += blockLength;
                            outIndex += blockLength;
                            run -= blockLength;
                        }

                        if (bestLength <= 10 && bestOffset <= 1024) // 2 byte op code  0x00 - 0x7f
                        {
                            if ((outIndex + run + 2) >= outLength)
                            {
                                return null;
                            }

                            outbuf[outIndex] = (byte)((((bestOffset >> 8) << 5) + ((bestLength - 3) << 2)) + run);
                            outbuf[outIndex + 1] = (byte)(bestOffset & 0xff);
                            outIndex += 2;
                        }
                        else if (bestLength <= 67 && bestOffset <= 16384)  // 3 byte op code 0x80 - 0xBF
                        {
                            if ((outIndex + run + 3) >= outLength)
                            {
                                return null;
                            }

                            outbuf[outIndex] = (byte)(0x80 + (bestLength - 4));
                            outbuf[outIndex + 1] = (byte)((run << 6) + (bestOffset >> 8));
                            outbuf[outIndex + 2] = (byte)(bestOffset & 0xff);
                            outIndex += 3;
                        }
                        else // 4 byte op code 0xC0 - 0xDF
                        {
                            if ((outIndex + run + 4) >= outLength)
                            {
                                return null;
                            }

                            outbuf[outIndex] = (byte)(((0xC0 + ((bestOffset >> 16) << 4)) + (((bestLength - 5) >> 8) << 2)) + run);
                            outbuf[outIndex + 1] = (byte)((bestOffset >> 8) & 0xff);
                            outbuf[outIndex + 2] = (byte)(bestOffset & 0xff);
                            outbuf[outIndex + 3] = (byte)((bestLength - 5) & 0xff);
                            outIndex += 4;
                        }

                        for (int i = 0; i < run; i++)
                        {
                            outbuf[outIndex] = input[lastwrot];
                            lastwrot++;
                            outIndex++;
                        }
                        lastwrot += bestLength;
                    }
                }

                index++;
                remaining--;
            }

            run = inputLength - lastwrot;
            // write the end data
            while (run > 3) // 1 byte literal op code 0xE0 - 0xFB
            {
                int blockLength = Math.Min(run & ~3, LiteralRunMaxLength);

                if ((outIndex + blockLength + 1) >= outLength)
                {
                    return null; // data did not compress so return null
                }

                outbuf[outIndex] = (byte)(0xE0 + ((blockLength / 4) - 1));
                outIndex++;

                Buffer.BlockCopy(input, lastwrot, outbuf, outIndex, blockLength);
                lastwrot += blockLength;
                outIndex += blockLength;
                run -= blockLength;
            }
            
            if ((outIndex + run + 1) >= outLength)
            {
                return null;
            }

            // 1 byte EOF op code 0xFC - 0xFF
            outbuf[outIndex] = (byte)(0xFC + run);
            outIndex++;

            for (int i = 0; i < run; i++)
            {
                outbuf[outIndex] = input[lastwrot];
                lastwrot++;
                outIndex++;
            }

            if (prefixLength)
            {
                int finalLength = outIndex + 4;
                if (finalLength >= inputLength)
                {
                    return null;
                }

                byte[] temp = new byte[finalLength];

                // Write the compressed length before the actual data in little endian byte order.
                temp[0] = (byte)(outIndex & 0xff);
                temp[1] = (byte)((outIndex >> 8) & 0xff);
                temp[2] = (byte)((outIndex >> 16) & 0xff);
                temp[3] = (byte)((outIndex >> 24) & 0xff);

                Buffer.BlockCopy(outbuf, 0, temp, 4, outIndex);
                outbuf = temp;
            }
            else
            {
                byte[] temp = new byte[outIndex]; // trim the outbuf array to it's actual length
                Buffer.BlockCopy(outbuf, 0, temp, 0, outIndex);
                outbuf = temp;
            }

            return outbuf;
        }
    }
}
