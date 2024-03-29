﻿/*
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
    static class DXTComp
    {
        #region Squish Decompression Code

        /* -----------------------------------------------------------------------------

        Copyright (c) 2006 Simon Brown                          si@sjbrown.co.uk

        Permission is hereby granted, free of charge, to any person obtaining
        a copy of this software and associated documentation files (the
        "Software"), to deal in the Software without restriction, including
        without limitation the rights to use, copy, modify, merge, publish,
        distribute, sublicense, and/or sell copies of the Software, and to
        permit persons to whom the Software is furnished to do so, subject to
        the following conditions:

        The above copyright notice and this permission notice shall be included
        in all copies or substantial portions of the Software.

        THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
        OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
        MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
        IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
        CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
        TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
        SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

        -------------------------------------------------------------------------- */

        /// <summary>
        /// Unpacks the DXT image.
        /// </summary>
        /// <param name="blocks">The byte array containing the compressed blocks.</param>
        /// <param name="offset">The offset of the data within the <paramref name="blocks"/> array.</param>
        /// <param name="width">The width of the final image.</param>
        /// <param name="height">The height of the final image.</param>
        /// <param name="dxt1">set to <c>true</c> if the image is DXT1.</param>
        /// <exception cref="System.ArgumentNullException">blocks</exception>
        public static unsafe byte[] UnpackDXTImage(byte[] blocks, int offset, int width, int height, bool dxt1)
        {
            if (blocks == null)
            {
                throw new ArgumentNullException("blocks");
            }

            byte[] pixelData = new byte[(width * height) * 4];

            fixed (byte* rgba = pixelData)
            {
                fixed (byte* pBlocks = &blocks[offset]) // fix the array in place
                {
                    int bytesPerBlock = dxt1 ? 8 : 16;
                    byte* targetRGBA = stackalloc byte[4 * 16];
                    byte* pBlock = pBlocks;
                    byte* sourcePixel;
                    byte* targetPixel;
                    for (int y = 0; y < height; y += 4)
                    {
                        for (int x = 0; x < width; x += 4)
                        {
                            // decompress the block.
                            Decompress(targetRGBA, pBlock, dxt1);

                            // write the decompressed pixels to the correct image locations
                            sourcePixel = targetRGBA;
                            for (int py = 0; py < 4; py++)
                            {
                                int sy = y + py;

                                for (int px = 0; px < 4; px++)
                                {
                                    // get the target location
                                    int sx = x + px;

                                    if (sx < width && sy < height)
                                    {
                                        targetPixel = rgba + 4 * ((width * sy) + sx);

                                        for (int p = 0; p < 4; p++)
                                        {
                                            *targetPixel++ = *sourcePixel++; // copy the target value
                                        }
                                    }
                                    else
                                    {
                                        // skip the pixel as its outside the range
                                        sourcePixel += 4;
                                    }
                                }
                            }

                            pBlock += bytesPerBlock;
                        }
                    }
                }
            }

            return pixelData;
        }

        /// <summary>
        /// Decompresses the DXT compressed block.
        /// </summary>
        /// <param name="rgba">The output rgba data.</param>
        /// <param name="block">The compressed block.</param>
        /// <param name="isDxt1">set to <c>true</c> if the image is DXT1.</param>
        private static unsafe void Decompress(byte* rgba, byte* block, bool isDxt1)
        {
            byte* colorBlock = block;
            byte* alphaBlock = block;

            if (isDxt1)
            {
                DecompressColor(rgba, colorBlock, true);
            }
            else
            {
                colorBlock = block + 8;
                DecompressColor(rgba, colorBlock, false);
                DecompressDXT3Alpha(rgba, alphaBlock);
            }

        }

        /// <summary>
        /// Unpacks 565 packed color values.
        /// </summary>
        /// <param name="packed">The packed values.</param>
        /// <param name="colors">The unpacked colors.</param>
        /// <returns></returns>
        private static unsafe int Unpack565(byte* packed, byte* colors)
        {
            int value = packed[0] | (packed[1] << 8);

            int red = (value >> 11) & 0x1f;
            int green = (value >> 5) & 0x3f;
            int blue = (value & 0x1f);

            colors[0] = (byte)((red << 3) | (red >> 2));
            colors[1] = (byte)((green << 2) | (green >> 4));
            colors[2] = (byte)((blue << 3) | (blue >> 2));
            colors[3] = 255;

            return value;
        }

        /// <summary>
        /// Decompresses the DXT color data.
        /// </summary>
        /// <param name="rgba">The output rgba data.</param>
        /// <param name="blocks">The compressed block.</param>
        /// <param name="isDxt1">set to <c>true</c> if the image is DXT1 to handle it's alpha channel.</param>
        private static unsafe void DecompressColor(byte* rgba, byte* blocks, bool isDxt1)
        {
            byte* codes = stackalloc byte[16];

            int a = Unpack565(blocks, codes);
            int b = Unpack565(blocks + 2, codes + 4);

            // unpack the midpoints
            for (int i = 0; i < 3; i++)
            {
                int c = codes[i];
                int d = codes[4 + i];

                if (isDxt1 && a <= b) // dxt1 alpha is a special case
                {
                    codes[8 + i] = (byte)((c + d) / 2);
                    codes[12 + i] = 0;
                }
                else
                {
                    // handle the other mask cases from FSHTool.

                    if (a > b)
                    {
                        codes[8 + i] = (byte)((2 * c + d) / 3);
                        codes[12 + i] = (byte)((c + 2 * d) / 3);
                    }
                    else
                    {
                        codes[8 + i] = (byte)((c + d) / 2);
                        codes[12 + i] = (byte)((c + d) / 2);
                    }

                }
            }


            // fill in alpha for the intermediate values
            codes[8 + 3] = 255;
            codes[12 + 3] = (isDxt1 && a <= b) ? (byte)0 : (byte)255;

            byte* indices = stackalloc byte[16];

            for (int i = 0; i < 4; i++)
            {
                byte* index = indices + 4 * i;
                byte packed = blocks[4 + i];

                index[0] = (byte)(packed & 3);
                index[1] = (byte)((packed >> 2) & 3);
                index[2] = (byte)((packed >> 4) & 3);
                index[3] = (byte)((packed >> 6) & 3);
            }
            // store out the colors
            for (int i = 0; i < 16; i++)
            {
                int offset = 4 * indices[i];
                int index = 4 * i;
                for (int j = 0; j < 4; j++)
                {
                    rgba[index + j] = codes[offset + j];
                }
            }
        }

        /// <summary>
        /// Decompresses the DXT3 compressed alpha.
        /// </summary>
        /// <param name="rgba">The output rgba values.</param>
        /// <param name="block">The compressed alpha block.</param>
        private static unsafe void DecompressDXT3Alpha(byte* rgba, byte* block)
        {
            for (int i = 0; i < 8; i++)
            {
                byte quant = block[i];

                // extract the values
                int lo = quant & 0x0f;
                int hi = quant & 0xf0;
                int index = 8 * i;
                // convert back up to bytes
                rgba[index + 3] = (byte)(lo | (lo << 4));
                rgba[index + 7] = (byte)(hi | (hi >> 4));
            }
        }
        #endregion

        #region FSHTool Compression code
        /*
         * The following code has been adapted from FSHTool version 1.22:
         *
         * ===========================================================================
         * FSHTOOL version 1.22 - (c) Denis Auroux 2002 - auroux@math.polytechnique.fr
         * ===========================================================================
         *
         * Second generation FSH/QFS decompressor/compressor to BMP format
         * Version 1.22 - copyright (c) Denis Auroux 1998-2002
         *
         * FSHTOOL is free software. It is distributed under the terms of the
         * GNU General Public License. No warranty.
         */

        public static unsafe byte[] CompressFSHToolDXT1(byte* scan0, int width, int height)
        {
            if ((width & 3) != 0 || (height & 3) != 0)
            {
                throw new NotSupportedException(Properties.Resources.DXT1InvalidSize);
            }

            byte[] comp = new byte[((width * height) / 2) + 2000];

            int stride = 4 * width;
            while ((stride & 4) > 0)
            {
                stride++;
            }
            int row, col, ofs, dxtRow;
            int blockWidth = width / 4;
            int blockHeight = height / 4;

            fixed (byte* ptr = comp)
            {
                uint* dxtPixels = stackalloc uint[16];

                for (int y = 0; y < blockHeight; y++)
                {
                    row = 4 * y;
                    for (int x = 0; x < blockWidth; x++)
                    {
                        col = 16 * x;
                        for (int i = 0; i < 4; i++)
                        {
                            dxtRow = 4 * i;
                            byte* p = (scan0 + ((row + i) * stride)) + col;
                            for (int j = 0; j < 4; j++)
                            {
                                ofs = 4 * j;
                                dxtPixels[dxtRow + j] = (uint)((p[ofs] + (p[ofs + 1] << 8)) + (p[ofs + 2] << 16));
                            }
                        }

                        PackDXT(dxtPixels, (ptr + ((2 * y) * width)) + (8 * x));
                    }
                }
            }


            return comp;
        }

        public static unsafe byte[] CompressFSHToolDXT3(byte* scan0, int width, int height)
        {
            byte[] comp = new byte[(width * height) + 2000];

            int blockHeight = height / 4;
            int blockWidth = width / 4;
            int row, col, ofs, dxtRow;


            int stride = 4 * width;
            while ((stride & 4) > 0)
            {
                stride++;
            }

            fixed (byte* ptr = comp)
            {
                uint* dxtPixels = stackalloc uint[16];

                for (int y = 0; y < blockHeight; y++)
                {
                    row = 4 * y;
                    for (int x = 0; x < blockWidth; x++)
                    {
                        col = 16 * x;
                        for (int i = 0; i < 4; i++)
                        {
                            dxtRow = 4 * i;
                            byte* p = (scan0 + ((row + i) * stride)) + col;

                            for (int j = 0; j < 4; j++)
                            {
                                ofs = 4 * j;
                                dxtPixels[dxtRow + j] = (uint)(p[ofs] + (p[ofs + 1] << 8) + (p[ofs + 2] << 16));
                            }
                        }

                        PackDXT(dxtPixels, ptr + ((row * width) + col) + 8);
                    }
                }

                for (int y = 0; y < blockHeight; y++)
                {
                    row = 4 * y;
                    for (int x = 0; x < blockWidth; x++)
                    {
                        ofs = 16 * x;
                        col = ofs + 3; // get the alpha offset
                        dxtRow = row * width;
                        for (int i = 0; i < 4; i++)
                        {
                            byte* p = (scan0 + ((row + i) * stride)) + col;
                            byte* tgt = ptr + (dxtRow + ofs) + 2 * i;

                            tgt[0] = (byte)(((p[0] & 0xf0) >> 4) + (p[4] & 0xf0));
                            tgt[1] = (byte)(((p[8] & 0xf0) >> 4) + (p[12] & 0xf0));
                        }
                    }
                }
            }


            return comp;
        }

        private static unsafe void PackDXT(uint* px, byte* dest)
        {
            uint[] uniq = new uint[0x10];

            int i, j;
            uint col1;
            uint col2;
            int nstep = 0;
            int bestErr = 0;
            uint bestCol1 = 0U;
            uint bestCol2 = 0U;
            int nColors = 0;

            for (i = 0; i < 0x10; i++)
            {
                col1 = px[i] & 0xf8fcf8U;
                j = 0;
                while (j < nColors)
                {
                    if (uniq[j] == col1)
                    {
                        break;
                    }
                    j++;
                }
                if (j == nColors)
                {
                    uniq[nColors++] = col1;
                }
            }
            if (nColors == 1)
            {
                bestCol1 = uniq[0];
                bestCol2 = uniq[0];
                bestErr = 0x3e8;
                nstep = 3;
            }
            else
            {
                bestErr = 0x40000000;
                for (i = 0; i < (nColors - 1); i++)
                {
                    for (j = i + 1; j < nColors; j++)
                    {
                        ulong dst;
                        int err = ScoreDXT(px, 2, uniq[i], uniq[j], &dst);
                        if (err < bestErr)
                        {
                            bestCol1 = uniq[i];
                            bestCol2 = uniq[j];
                            nstep = 2;
                            bestErr = err;
                        }
                        err = ScoreDXT(px, 3, uniq[i], uniq[j], &dst);
                        if (err < bestErr)
                        {
                            bestCol1 = uniq[i];
                            bestCol2 = uniq[j];
                            nstep = 3;
                            bestErr = err;
                        }
                    }
                }
            }
            byte* c1 = (byte*)&col1;
            byte* c2 = (byte*)&col2;
            col1 = bestCol1;
            col2 = bestCol2;
            ushort* sPtr = (ushort*)dest;
            sPtr[0] = (ushort)(((c1[0] >> 3) + ((c1[1] >> 2) << 5)) + ((c1[2] >> 3) << 11));
            sPtr[1] = (ushort)(((c2[0] >> 3) + ((c2[1] >> 2) << 5)) + ((c2[2] >> 3) << 11));
            if ((sPtr[0] > sPtr[1]) ^ (nstep == 3))
            {
                ushort temp = sPtr[0];
                sPtr[0] = sPtr[1];
                sPtr[1] = temp;
                bestCol1 = col2;
                bestCol2 = col1;
            }

            ScoreDXT(px, nstep, bestCol1, bestCol2, (ulong*)(dest + 4));
        }

        private static unsafe int ScoreDXT(uint* px, int nstep, uint col1, uint col2, ulong* pack)
        {
            int[] vec = new int[3];
            int[] vdir = new int[3];

            byte* c1 = (byte*)&col1;
            byte* c2 = (byte*)&col2;

            vdir[0] = c2[0] - c1[0];
            vdir[1] = c2[1] - c1[1];
            vdir[2] = c2[2] - c1[2];

            int v2 = ((vdir[0] * vdir[0]) + (vdir[1] * vdir[1])) + (vdir[2] * vdir[2]);

            int score = 0;
            pack[0] = 0L;
            byte* ptr = (byte*)(px + 15);
            int i = 15;
            int choice;
            while (i >= 0)
            {

                vec[0] = ptr[0] - c1[0];
                vec[1] = ptr[1] - c1[1];
                vec[2] = ptr[2] - c1[2];
                int xa2 = ((vec[0] * vec[0]) + (vec[1] * vec[1])) + (vec[2] * vec[2]);
                int xav = ((vec[0] * vdir[0]) + (vec[1] * vdir[1])) + (vec[2] * vdir[2]);
                if (v2 > 0)
                {
                    choice = ((nstep * xav) + (v2 >> 1)) / v2;
                }
                else
                {
                    choice = 0;
                }

                if (choice < 0)
                {
                    choice = 0;
                }
                else if (choice > nstep)
                {
                    choice = nstep;
                }

                score += (xa2 - (((2 * choice) * xav) / nstep)) + (((choice * choice) * v2) / (nstep * nstep));
                pack[0] = pack[0] << 2;

                if (choice == nstep)
                {
                    pack[0] += 1UL;
                }
                else if (choice > 0)
                {
                    pack[0] += (ulong)(choice + 1L);
                }

                i--;
                ptr = (byte*)(px + i);
            }

            return score;
        }
        #endregion
    }
}
