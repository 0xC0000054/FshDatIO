﻿using System;
using System.IO;

namespace FshDatIO
{
    static class QfsComp
    {
        /// <summary>
        /// Decompresses an QFS Compressed File
        /// </summary>
        /// <param name="compressedData">The byte array to decompress</param>
        /// <returns>A byte array containing the decompressed data</returns>
        public static byte[] Decompress(byte[] compressedData)
        {
            if (compressedData == null)
            {
                throw new ArgumentNullException("compressedData");
            }

            int startOffset = 0;

            if ((compressedData[0] & 0xFE) != 0x10 || compressedData[1] != 0xFB)
            {
                if (compressedData[4] != 0x10 && compressedData[5] != 0xFB)
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
                ccbyte1 = compressedData[index++];

                if (ccbyte1 >= 0xE0) // 1 byte op code 0xE0 - 0xFB
                {
                    plainCount = ((ccbyte1 & 0x1F) << 2) + 4;
                    copyCount = 0;
                    copyOffset = 0;
                }
                else if (ccbyte1 >= 0xC0) // 4 byte op code 0xC0 - 0xDF
                {
                    ccbyte2 = compressedData[index++];
                    ccbyte3 = compressedData[index++];
                    ccbyte4 = compressedData[index++];

                    plainCount = (ccbyte1 & 3);
                    copyCount = ((ccbyte1 & 0x0C) << 6) + ccbyte4 + 5;
                    copyOffset = (((ccbyte1 & 0x10) << 12) + (ccbyte2 << 8)) + ccbyte3 + 1;
                }
                else if (ccbyte1 >= 0x80) // 3 byte op code 0x80 - 0xBF
                {
                    ccbyte2 = compressedData[index++];
                    ccbyte3 = compressedData[index++];

                    plainCount = (ccbyte2 & 0xC0) >> 6;
                    copyCount = (ccbyte1 & 0x3F) + 4;
                    copyOffset = ((ccbyte2 & 0x3F) << 8) + ccbyte3 + 1;
                }
                else // 2 byte op code 0x00 - 0x7F
                {
                    ccbyte2 = compressedData[index++];

                    plainCount = (ccbyte1 & 3);
                    copyCount = ((ccbyte1 & 0x1C) >> 2) + 3;
                    copyOffset = ((ccbyte1 >> 5) << 8) + ccbyte2 + 1;
                }

                for (int i = 0; i < plainCount; i++)
                {
                    unCompressedData[outIndex] = compressedData[index];
                    index++;
                    outIndex++;
                }

                if (copyCount > 0)
                {
                    int srcIndex = outIndex - copyOffset;

                    for (int i = 0; i < copyCount; i++)
                    {
                        unCompressedData[outIndex] = unCompressedData[srcIndex];
                        srcIndex++;
                        outIndex++;
                    }
                }
            }

            // Write the trailing bytes.
            if (index < length && outIndex < outLength)
            {
                // 1 byte EOF op code 0xFC - 0xFF.
                plainCount = (compressedData[index++] & 3);

                for (int i = 0; i < plainCount; i++)
                {
                    unCompressedData[outIndex] = compressedData[index];
                    index++;
                    outIndex++;
                }
            }

            return unCompressedData;
        }

        const int QfsMaxIterCount = 50;
        const int QfsMaxBlockSize = 1028;
        const int CompMaxLen = 131072; // FshTool's WINDOWLEN
        const int CompMask = CompMaxLen - 1;  // Fshtool's WINDOWMASK
        const int LiteralRunMaxLength = 27;
        /// <summary>
        /// Compresses the input byte array with QFS compression
        /// </summary>
        /// <param name="input">The input byte array to compress</param>
        /// <param name="prefixLength">If set to true prefix the size of the compressed data, as is used by SC4; otherwise false.</param>
        /// <returns>A byte array containing compressed data or null if the compression fails.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="input"/> is null.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1814:PreferJaggedArraysOverMultidimensional", MessageId = "Body"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        public static byte[] Compress(byte[] input, bool prefixLength)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input", "input byte array is null.");
            }

            int inlen = input.Length;
            byte[] inbuf = new byte[inlen + 1028]; // 1028 byte safety buffer
            Buffer.BlockCopy(input, 0, inbuf, 0, input.Length);

            int[] similar_rev = new int[CompMaxLen];
            int[,] last_rev = new int[256, 256];

            for (int i = 0; i < CompMaxLen; i++)
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

            byte[] outbuf = new byte[inlen + 2048];
            outbuf[0] = 0x10;
            outbuf[1] = 0xfb;
            outbuf[2] = (byte)((inlen >> 16) & 0xff);
            outbuf[3] = (byte)((inlen >> 8) & 0xff);
            outbuf[4] = (byte)(inlen & 0xff);
            int outIndex = 5;

            int run = 0;
            int lastwrot = 0;
            int index = 0;

            for (index = 0; index < inlen; index++)
            {
                int offs = last_rev[inbuf[index], inbuf[index + 1]];
                similar_rev[index & CompMask] = offs;
                last_rev[inbuf[index], inbuf[index + 1]] = index;

                if (index < lastwrot)
                {
                    continue;
                }
                
                int bestLength = 0;            
                int bestOffset = 0;
                int iterCount = 0;
                while (((offs >= 0) && ((index - offs) < CompMaxLen)) && (iterCount < QfsMaxIterCount))
                {
                    run = 2;
                    while ((inbuf[index + run] == inbuf[offs + run]) && (run < QfsMaxBlockSize))
                    {
                        run++;
                    }
                    if (run > bestLength)
                    {
                        bestLength = run;
                        bestOffset = index - offs;
                    }
                    offs = similar_rev[offs & CompMask];
                    iterCount++;
                }
                if (bestLength > (inlen - index))
                {
                    bestLength = index - inlen;
                }
                if (bestLength <= 2)
                {
                    bestLength = 0;
                }
                else if (bestLength == 3 && bestOffset > 1024)
                {
                    bestLength = 0;
                }
                else if (bestLength == 4 && bestOffset > 16384)
                {
                    bestLength = 0;
                }
                if (bestLength > 0)
                {
                    while ((index - lastwrot) >= 4) // 1 byte op code 0xE0 - 0xFB
                    {
                        run = ((index - lastwrot) / 4) - 1;
                        if (run > LiteralRunMaxLength)
                        {
                            run = LiteralRunMaxLength;
                        }
                        outbuf[outIndex++] = (byte)(0xe0 + run);
                        
                        int blockLength = (run * 4) + 4;
                        if ((outIndex + blockLength) >= outbuf.Length)
                        {
                            return null;// data did not compress so return null
                        }
                        Buffer.BlockCopy(inbuf, lastwrot, outbuf, outIndex, blockLength);
                        lastwrot += blockLength;
                        outIndex += blockLength;
                    }
                    run = index - lastwrot;
                    if ((bestLength <= 10) && (bestOffset <= 1024)) // 2 byte op code  0x00 - 0x7f
                    {
                        outbuf[outIndex++] = (byte)(((((bestOffset - 1) >> 8) << 5) + ((bestLength - 3) << 2)) + run);
                        outbuf[outIndex++] = (byte)((bestOffset - 1) & 0xff);
                        if ((outIndex + run) >= outbuf.Length)
                        {
                            return null;// data did not compress so return null
                        }
                        while (run-- > 0)
                        {
                            outbuf[outIndex++] = inbuf[lastwrot++];
                        }
                        lastwrot += bestLength;
                    }
                    else if ((bestLength <= 67) && (bestOffset <= 16384))  // 3 byte op code 0x80 - 0xBF
                    {
                        outbuf[outIndex++] = (byte)(0x80 + (bestLength - 4));
                        outbuf[outIndex++] = (byte)((run << 6) + ((bestOffset - 1) >> 8));
                        outbuf[outIndex++] = (byte)((bestOffset - 1) & 0xff);
                        if ((outIndex + run) >= outbuf.Length)
                        {
                            return null;// data did not compress so return null
                        }
                        while (run-- > 0)
                        {
                            outbuf[outIndex++] = inbuf[lastwrot++];
                        }
                        lastwrot += bestLength;
                    }
                    else if ((bestLength <= QfsMaxBlockSize) && (bestOffset < CompMaxLen)) // 4 byte op code 0xC0 - 0xFB
                    {
                        bestOffset--;
                        outbuf[outIndex++] = (byte)(((0xc0 + ((bestOffset >> 0x10) << 4)) + (((bestLength - 5) >> 8) << 2)) + run);
                        outbuf[outIndex++] = (byte)((bestOffset >> 8) & 0xff);
                        outbuf[outIndex++] = (byte)(bestOffset & 0xff);
                        outbuf[outIndex++] = (byte)((bestLength - 5) & 0xff);
                        if ((outIndex + run) >= outbuf.Length)
                        {
                            return null;
                        }
                        while (run-- > 0)
                        {
                            outbuf[outIndex++] = inbuf[lastwrot++];
                        }
                        lastwrot += bestLength;
                    }
                }
            }
            index = inlen;
            // write the end data
            while ((index - lastwrot) >= 4) // 1 byte op code 0xE0 - 0xFB
            {
                run = ((index - lastwrot) / 4) - 1;
                if (run > LiteralRunMaxLength)
                {
                    run = LiteralRunMaxLength;
                }
                outbuf[outIndex++] = (byte)(0xe0 + run);
                int blockLength = (run * 4) + 4;

                if ((outIndex + blockLength) >= outbuf.Length)
                {
                    return null; // data did not compress so return null
                }

                Buffer.BlockCopy(inbuf, lastwrot, outbuf, outIndex, blockLength);
                lastwrot += blockLength;
                outIndex += blockLength;
            }
            run = index - lastwrot;

            if ((outIndex + run) >= outbuf.Length) // add in the remaining data length to check for available space
            {
                return null;
            }

            // 1 byte EOF op code 0xFC - 0xFF 
            outbuf[outIndex++] = (byte)(0xfc + run);

            while (run-- > 0)
            {
                outbuf[outIndex++] = inbuf[lastwrot++];
            }

            if (prefixLength)
            {
                byte[] temp = new byte[outIndex + 4];

                Array.Copy(BitConverter.GetBytes(outIndex), temp, 4); // write the compressed length before the actual data
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
