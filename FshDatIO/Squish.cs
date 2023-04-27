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
using System.Runtime.InteropServices;
using System.Security.Permissions;

namespace FshDatIO
{
    internal enum SquishFlags
    {
        kDxt1 = (1 << 0),		// Use DXT1 compression.
        kDxt3 = (1 << 1),		// Use DXT3 compression.
        kDxt5 = (1 << 2), 		// Use DXT5 compression.

        kColourClusterFit = (1 << 3),		// Use a slow but high quality colour compressor (the default).
        kColourRangeFit = (1 << 4),		// Use a fast but low quality colour compressor.

        kColourMetricPerceptual = (1 << 5),		// Use a perceptual metric for colour error (the default).
        kColourMetricUniform = (1 << 6),		// Use a uniform metric for colour error.

        kWeightColourByAlpha = (1 << 7),		// Weight the colour by alpha during cluster fit (disabled by default).

        kColourIterativeClusterFit = (1 << 8),		// Use a very slow but very high quality colour compressor.
    }
    
    static class Squish
    {
        [System.Security.SuppressUnmanagedCodeSecurity]
        private static class Squish_32
        {
            [DllImport("squish_Win32.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1060:MovePInvokesToNativeMethodsClass")]
            internal static extern unsafe void CompressImage(byte* rgba, int width, int height, byte* blocks, int flags);
        }
                
        [System.Security.SuppressUnmanagedCodeSecurity]
        private static class Squish_64
        {
            [DllImport("squish_x64.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1060:MovePInvokesToNativeMethodsClass")]
            internal static extern unsafe void CompressImage(byte* rgba, int width, int height, byte* blocks, int flags);
        }

        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        public static unsafe byte[] CompressImage(byte* scan0, int srcStride, int width, int height, int flags)
        {

            byte[] pixelData = new byte[width * height * 4];

            int dstStride = width * 4;
            fixed (byte* ptr = pixelData)
            {
                for (int y = 0; y < height; y++)
                {
                    byte* src = scan0 + (y * srcStride);
                    byte* dst = ptr + (y * dstStride);
                    for (int x = 0; x < width; x++)
                    {
                        dst[0] = src[2];
                        dst[1] = src[1];
                        dst[2] = src[0];
                        dst[3] = src[3];

                        src += 4;
                        dst += 4;
                    }
                }
            }

            // Compute size of compressed block area, and allocate 
            int blockCount = ((width + 3) / 4) * ((height + 3) / 4);
            int blockSize = ((flags & (int)SquishFlags.kDxt1) != 0) ? 8 : 16;

            // Allocate room for compressed blocks
            // with 16 bytes of padding after the compressed image data.
            
            byte[] blockData = new byte[(blockCount * blockSize) + 16];

            // Invoke squish::CompressImage() with the required parameters
            CompressImageWrapper(pixelData, width, height, blockData, flags);

            // Return our block data to caller..
            return blockData;
        }

        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        private static unsafe void CompressImageWrapper(byte[] rgba, int width, int height, byte[] blocks, int flags)
        {
            fixed (byte* RGBA = rgba)
            {
                fixed (byte* Blocks = blocks)
                {
                    if (IntPtr.Size == 8)
                    {
                        Squish_64.CompressImage(RGBA, width, height, Blocks, flags);
                    }
                    else
                    {
                        Squish_32.CompressImage(RGBA, width, height, Blocks, flags);
                    }
                }
            }
        }
        
    }
}
