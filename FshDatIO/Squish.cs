using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;
using System.Drawing;
using System.Diagnostics.CodeAnalysis;
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
        

        private static bool Is64Bit()
        {
            return (Marshal.SizeOf(IntPtr.Zero) == 8);
        }

        private static class Squish_32
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1060:MovePInvokesToNativeMethodsClass"), DllImport("Squish_Win32.dll")]
            internal static extern unsafe void SquishCompressImage(byte* rgba, int width, int height, byte* blocks, int flags);
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1060:MovePInvokesToNativeMethodsClass"), DllImport("squish_Win32.dll")]
            internal static extern unsafe void SquishDecompressImage(byte* rgba, int width, int height, byte* blocks, int flags);
        }

        private static class Squish_64
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1060:MovePInvokesToNativeMethodsClass"), DllImport("squish_x64.dll")]
            internal static extern unsafe void SquishCompressImage(byte* rgba, int width, int height, byte* blocks, int flags);
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1060:MovePInvokesToNativeMethodsClass"), DllImport("squish_x64.dll")]
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

        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        public static unsafe byte[] CompressImage(Bitmap image, int flags)
        {
            if (image == null)
                throw new ArgumentNullException("image", "image is null.");

            byte[] pixelData = new byte[image.Width * image.Height * 4];

            BitmapData data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            try
            {
                byte* scan0 = (byte*)data.Scan0.ToPointer();
                int srcStride = data.Stride;
                int dstStride = (image.Width * 4);
                fixed (byte* ptr = pixelData)
                {
                    for (int y = 0; y < image.Height; y++)
                    {
                        byte* p = scan0 + (y * srcStride);
                        byte* q = ptr + (y * dstStride);
                        for (int x = 0; x < image.Width; x++)
                        {
                            q[0] = p[2];
                            q[1] = p[1];
                            q[2] = p[0];
                            q[3] = p[3];

                            p += 4;
                            q += 4;
                        }
                    }
                }
            }
            finally
            {
                image.UnlockBits(data);
            }

            // Compute size of compressed block area, and allocate 
            int blockCount = ((image.Width + 3) / 4) * ((image.Height + 3) / 4);
            int blockSize = ((flags & (int)SquishFlags.kDxt1) != 0) ? 8 : 16;

            // Allocate room for compressed blocks
            byte[] blockData = new byte[blockCount * blockSize];

            // Invoke squish::CompressImage() with the required parameters
            CompressImageWrapper(pixelData, image.Width, image.Height, blockData, flags);

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
                    if (Is64Bit())
                    {
                        Squish_64.SquishCompressImage(RGBA, width, height, Blocks, flags);
                    }
                    else
                    {
                        Squish_32.SquishCompressImage(RGBA, width, height, Blocks, flags);
                    }
                }
            }
        }
        
    }
}
