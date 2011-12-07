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

        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        private static Bitmap BlendDXTBitmap(Bitmap color, Bitmap alpha)
        {
            if (color == null)
            {
                throw new ArgumentNullException("color", "The color bitmap must not be null.");
            }

            if (alpha == null)
            {
                throw new ArgumentNullException("alpha", "The alpha bitmap must not be null.");
            }
            
            if (color.Size != alpha.Size)
            {
                throw new ArgumentException("The bitmap and alpha must be equal size");
            }

            Bitmap image = null;
            Bitmap temp = null;
            try
            {
               
                temp = new Bitmap(color.Width, color.Height, PixelFormat.Format32bppArgb);
                
                
                Rectangle tempRect = new Rectangle(0, 0, temp.Width, temp.Height);
                BitmapData colordata = color.LockBits(new Rectangle(0, 0, color.Width, color.Height), ImageLockMode.ReadOnly, color.PixelFormat);
                BitmapData alphadata = alpha.LockBits(new Rectangle(0, 0, alpha.Width, alpha.Height), ImageLockMode.ReadOnly, alpha.PixelFormat);
                BitmapData bdata = temp.LockBits(tempRect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                IntPtr scan0 = bdata.Scan0;
                unsafe
                {
                    int clrBpp = (Bitmap.GetPixelFormatSize(color.PixelFormat) / 8);
                    int alphaBpp = (Bitmap.GetPixelFormatSize(alpha.PixelFormat) / 8);

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
                color.UnlockBits(colordata);
                alpha.UnlockBits(alphadata);
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
        /// <summary>
        /// Gets or sets a value indicating whether the file will be compressed with QFS compression.
        /// </summary>
        /// <value>
        ///   <c>true</c> if compressed; otherwise, <c>false</c>.
        /// </value>
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
                        ms.Write(BitConverter.GetBytes((ushort)bmp.Width), 0, 2); // write width
                        ms.Write(BitConverter.GetBytes((ushort)bmp.Height), 0, 2); //write height
                        for (int m = 0; m < 4; m++)
                        {
                            ms.Write(BitConverter.GetBytes((ushort)0), 0, 2);// write misc data
                        }

                        if (code == 0x60) //DXT1
                        {
                            Bitmap temp = BlendDXTBitmap(bmp, alpha);
                            byte[] data = new byte[temp.Width * temp.Height * 4];
                            int flags = (int)SquishFlags.kDxt1;
                            flags |= (int)SquishFlags.kColourIterativeClusterFit;
                            data = Squish.CompressImage(temp, flags);
                            ms.Write(data, 0, data.Length);
                        }
                        else if (code == 0x61) // DXT3
                        {
                            Bitmap temp = BlendDXTBitmap(bmp, alpha);
                            byte[] data = new byte[temp.Width * temp.Height * 4];
                            int flags = (int)SquishFlags.kDxt3;
                            flags |= (int)SquishFlags.kColourIterativeClusterFit;
                            data = Squish.CompressImage(temp, flags);
                            ms.Write(data, 0, data.Length);
                        }

                    }

                    ms.Position = 4L;
                    ms.Write(BitConverter.GetBytes((int)ms.Length), 0, 4); // write the files length
                    if (compress)
                    {
                        byte[] rawData = ms.ToArray();
                        byte[] compbuf = QfsComp.Comp(rawData);
                        if ((compbuf != null) && (compbuf.LongLength < ms.Length))
                        {
                            output.Write(compbuf, 0, compbuf.Length);
                        }
                        else
                        {
                            compress = false;
                            output.Write(rawData, 0, rawData.Length);
                        }
                    }
                    else
                    {
                        ms.WriteTo(output); // write the memory stream to the file
                    }
                    
                }
            }
        }

    }
}
