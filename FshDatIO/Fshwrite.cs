using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.IO;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Security.Permissions;

namespace FshDatIO
{
    internal class Fshwrite
    {
        public Fshwrite()
        {
            bmplist = new List<Bitmap>();
            alphalist = new List<Bitmap>();
            dirnames = new List<byte[]>();
            codelist = new List<int>();
        }
        private enum SquishCompFlags
        {
            //! Use DXT1 compression.
            kDxt1 = (1 << 0),

            //! Use DXT3 compression.
            kDxt3 = (1 << 1),

            //! Use DXT5 compression.
            kDxt5 = (1 << 2),

            //! Use a very slow but very high quality colour compressor.
            kColourIterativeClusterFit = (1 << 8),

            //! Use a slow but high quality colour compressor (the default).
            kColourClusterFit = (1 << 3),

            //! Use a fast but low quality colour compressor.
            kColourRangeFit = (1 << 4),

            //! Use a perceptual metric for colour error (the default).
            kColourMetricPerceptual = (1 << 5),

            //! Use a uniform metric for colour error.
            kColourMetricUniform = (1 << 6),

        }
        
        private static byte[] SwapRGB(byte[] bytes)
        {
            Byte tmp;
            for (int x = 0; x < bytes.GetLength(0); x += 4)
            {
                tmp = bytes[(x + 2)];
                bytes[x + 2] = bytes[x];
                bytes[x] = tmp;
            }
            return bytes;
        }
        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        private static byte[] CompressImage(Bitmap image, int flags)
        {
            if (image == null)
                throw new ArgumentNullException("image", "image is null.");

            byte[] pixelData = new byte[image.Width * image.Height * 4];

            BitmapData data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            int len = data.Stride * data.Height;
            byte[] temp = new byte[len];
            Marshal.Copy(data.Scan0, temp, 0, len);

            image.UnlockBits(data);

            pixelData = SwapRGB(temp);


            // Compute size of compressed block area, and allocate 
            int blockCount = ((image.Width + 3) / 4) * ((image.Height + 3) / 4);
            int blockSize = ((flags & (int)SquishCompFlags.kDxt1) != 0) ? 8 : 16;

            // Allocate room for compressed blocks
            byte[] blockData = new byte[blockCount * blockSize];

            // Invoke squish::CompressImage() with the required parameters
            CompressImageWrapper(pixelData, image.Width, image.Height, blockData, flags);

            // Return our block data to caller..
            return blockData;
        }
        private static bool Is64bit()
        {
            return (IntPtr.Size == 8);
        }
        [System.Security.SuppressUnmanagedCodeSecurity()]
        private static class Squish_32
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1060:MovePInvokesToNativeMethodsClass"), DllImport("Squish_Win32.dll")]
            internal static extern unsafe void SquishCompressImage(byte* rgba, int width, int height, byte* blocks, int flags);
        }
        [System.Security.SuppressUnmanagedCodeSecurity()]
        private static class Squish_64
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1060:MovePInvokesToNativeMethodsClass"), DllImport("squish_x64.dll")]
            internal static extern unsafe void SquishCompressImage(byte* rgba, int width, int height, byte* blocks, int flags);
        }
        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        private static unsafe void CompressImageWrapper(byte[] rgba, int width, int height, byte[] blocks, int flags)
        {
            fixed (byte* RGBA = rgba)
            {
                fixed (byte* Blocks = blocks)
                {
                    if (Is64bit())
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
        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        private static Bitmap BlendDXTBmp(Bitmap colorbmp, Bitmap alphabmp)
        {
            Bitmap image = null;
            Bitmap temp = null;
            try
            {
                if (colorbmp != null && alphabmp != null)
                {
                    temp = new Bitmap(colorbmp.Width, colorbmp.Height, PixelFormat.Format32bppArgb);
                }
                if (colorbmp.Size != alphabmp.Size)
                {
                    throw new ArgumentException("The bitmap and alpha must be equal size");
                }
                Rectangle tempRect = new Rectangle(0, 0, temp.Width, temp.Height);
                BitmapData colordata = colorbmp.LockBits(new Rectangle(0, 0, colorbmp.Width, colorbmp.Height), ImageLockMode.ReadOnly, colorbmp.PixelFormat);
                BitmapData alphadata = alphabmp.LockBits(new Rectangle(0, 0, alphabmp.Width, alphabmp.Height), ImageLockMode.ReadOnly, alphabmp.PixelFormat);
                BitmapData bdata = temp.LockBits(tempRect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                IntPtr scan0 = bdata.Scan0;
                unsafe
                {
                    int clrBpp = (Bitmap.GetPixelFormatSize(colorbmp.PixelFormat) / 8);
                    int alphaBpp = (Bitmap.GetPixelFormatSize(alphabmp.PixelFormat) / 8);

                    byte* clrdata = (byte*)(void*)colordata.Scan0;
                    byte* aldata = (byte*)(void*)alphadata.Scan0;
                    byte* destdata = (byte*)(void*)scan0;
                    int offset = bdata.Stride - temp.Width * 4;
                    int clroffset = colordata.Stride - temp.Width * clrBpp;
                    int aloffset = alphadata.Stride - temp.Width * alphaBpp;
                    for (int y = 0; y < temp.Height; y++)
                    {
                        for (int x = 0; x < temp.Width; x++)
                        {
                            destdata[3] = aldata[0];
                            destdata[0] = clrdata[0];
                            destdata[1] = clrdata[1];
                            destdata[2] = clrdata[2];


                            destdata += 4;
                            clrdata += clrBpp;
                            aldata += alphaBpp;
                        }
                        destdata += offset;
                        clrdata += clroffset;
                        aldata += aloffset;
                    }

                }
                colorbmp.UnlockBits(colordata);
                alphabmp.UnlockBits(alphadata);
                temp.UnlockBits(bdata);

                image = temp.Clone(tempRect, temp.PixelFormat);
            }
            finally
            {
                if (temp != null)
                {
                    temp.Dispose();
                    temp = null;
                }
            }
            return image;
        }

        private List<Bitmap> bmplist = null;
        private List<Bitmap> alphalist = null;
        private List<byte[]> dirnames = null;
        private List<int> codelist = null;
        private bool compress = false;

        private static int GetBmpDataSize(Bitmap bmp, int code)
        {
            int ret = -1;
            switch (code)
            {
                case 0x60:
                    ret = (bmp.Width * bmp.Height / 2); //Dxt1
                    break;
                case 0x61:
                    ret = (bmp.Width * bmp.Height); //Dxt3
                    break;
            }
            return ret;
        }
        public List<Bitmap> alpha
        {
            get 
            {
                return alphalist;
            }
        }
        public List<Bitmap> bmp
        {
            get
            {
                return bmplist;
            }
        }
        public List<byte[]> dir
        {
            get
            {
                return dirnames;
            }
        }
        public List<int> code
        {
            get
            {
                return codelist;
            }
        }
        public bool Compress
        {
            get
            {
                return compress;
            }
            set
            {
                compress = value;
            }
        }
        /// <summary>
        /// The function that writes the fsh
        /// </summary>
        /// <param name="output">The output file to write to</param>
        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        public unsafe void WriteFsh(Stream output)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                if (bmplist != null && bmplist.Count > 0 && alphalist != null && dirnames != null && codelist != null)
                {
                    //write header
                    ms.Write(Encoding.ASCII.GetBytes("SHPI"), 0, 4); // write SHPI id
                    ms.Write(BitConverter.GetBytes(0), 0, 4); // placeholder for the length
                    ms.Write(BitConverter.GetBytes(bmplist.Count), 0, 4); // write the number of bitmaps in the list
                    ms.Write(Encoding.ASCII.GetBytes("G264"), 0, 4); // 

                    int fshlen = 16 + (8 * bmplist.Count); // fsh length
                    for (int c = 0; c < bmplist.Count; c++)
                    {
                        //write directory
                       // Debug.WriteLine("bmp = " + c.ToString() + " offset = " + fshlen.ToString());
                        ms.Write(dir[c], 0, 4); // directory id
                        ms.Write(BitConverter.GetBytes(fshlen), 0, 4); // Write the Entry offset 

                        fshlen += 16; // skip the entry header length
                        int bmplen = GetBmpDataSize(bmplist[c], codelist[c]);
                        fshlen += bmplen; // skip the bitmap length
                    }
                    for (int b = 0; b < bmplist.Count; b++)
                    {
                        Bitmap bmp = bmplist[b];
                        Bitmap alpha = alphalist[b];
                        int code = codelist[b];
                        // write entry header
                        ms.Write(BitConverter.GetBytes(code), 0, 4); // write the Entry bitmap code
                        ms.Write(BitConverter.GetBytes((short)bmp.Width), 0, 2); // write width
                        ms.Write(BitConverter.GetBytes((short)bmp.Height), 0, 2); //write height
                        for (int m = 0; m < 4; m++)
                        {
                            ms.Write(BitConverter.GetBytes((short)0), 0, 2);// write misc data
                        }

                        if (code == 0x60) //DXT1
                        {
                            Bitmap temp = BlendDXTBmp(bmp, alpha);
                            byte[] data = new byte[temp.Width * temp.Height * 4];
                            int flags = (int)SquishCompFlags.kDxt1;
                            flags |= (int)SquishCompFlags.kColourIterativeClusterFit;
                            data = CompressImage(temp, flags);
                            ms.Write(data, 0, data.Length);
                        }
                        else if (code == 0x61) // DXT3
                        {
                            Bitmap temp = BlendDXTBmp(bmp, alpha);
                            byte[] data = new byte[temp.Width * temp.Height * 4];
                            int flags = (int)SquishCompFlags.kDxt3;
                            flags |= (int)SquishCompFlags.kColourIterativeClusterFit;
                            data = CompressImage(temp, flags);
                            ms.Write(data, 0, data.Length);
                        }

                    }

                    ms.Position = 4L;
                    ms.Write(BitConverter.GetBytes((int)ms.Length), 0, 4); // write the files length
                    if (compress)
                    {
                        ms.Position = 0L;
                        byte[] compbuf = QfsComp.Comp(ms);
                        if ((compbuf != null) && (compbuf.Length < (int)ms.Length))
                        {
                            using (MemoryStream ms2 = new MemoryStream(compbuf))
                            {
                                ms2.WriteTo(output);
                            }
                        }
                        else
                        {
                            ms.Position = 0L;
                            compress = false;
                            ms.WriteTo(output);
                        }
                    }
                    else
                    {
                        ms.Position = 0L;
                        ms.WriteTo(output); // write the memory stream to the file
                    }
                    
                }
            }
        }

    }
}
