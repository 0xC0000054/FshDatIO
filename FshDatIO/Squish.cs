﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;
using System.Drawing;

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
        

        private static bool Is64Bit()
        {
            return (Marshal.SizeOf(IntPtr.Zero) == 8);
        }

        private static class Squish_32
        {
            [DllImport("squish_Win32.dll")]
            internal static extern unsafe void SquishDecompressImage(byte* rgba, int width, int height, byte* blocks, int flags);
        }

        private static class Squish_64
        {
            [DllImport("squish_x64.dll")]
            internal static extern unsafe void SquishDecompressImage(byte* rgba, int width, int height, byte* blocks, int flags);
        }
        private static unsafe void CallDecompressImage(byte[] rgba, int width, int height, byte[] blocks, int flags)
        {
            fixed (byte* pRGBA = rgba)
            {
                fixed (byte* pBlocks = blocks)
                {
                    if (Is64Bit())
                        Squish_64.SquishDecompressImage(pRGBA, width, height, pBlocks, flags);
                    else
                        Squish_32.SquishDecompressImage(pRGBA, width, height, pBlocks, flags);
                }
            }
        }

        public static byte[] DecompressImage(byte[] blocks, int width, int height, int flags)
        {
            // Allocate room for decompressed output
            byte[] pixelOutput = new byte[width * height * 4];

            // Invoke squish::DecompressImage() with the required parameters
            CallDecompressImage(pixelOutput, width, height, blocks, flags);

            // Return our pixel data to caller..
            return pixelOutput;
        }

        
    }
}