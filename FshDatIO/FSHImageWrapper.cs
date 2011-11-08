using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSHLib;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using FshDatIO.Properties;

namespace FshDatIO
{
    public sealed class FSHImageWrapper : IDisposable
    {
        private FSHHeader header;
        private List<BitmapEntry> bitmaps;
        private FSHDirEntry[] dirs;
        private bool isCompressed;
        private byte[] rawData;

        /// <summary>
        /// Gets the list of bitmaps.
        /// </summary>
        public List<BitmapEntry> Bitmaps
        {
            get
            {
                return bitmaps;
            }
        }

        public bool IsCompressed
        {
            get
            {
                return isCompressed;
            }
            set
            {
                isCompressed = value;
            }
        }

        public byte[] RawData
        {
            get
            {
                return rawData;
            }
        }

        private MemoryStream Decompress(Stream stream)
        {
            byte[] packbuf = new byte[2];
            stream.Read(packbuf, 0, 2);

            if (packbuf[0] == 16 && packbuf[1] == 0xfb) // NFS 1 uses this offset
            {            
                this.isCompressed = true;
                return new MemoryStream(QfsComp.Decomp(stream, 0, (int)stream.Length));
            }
            else
            {
                stream.Position = 4L; // SimCity 4 uses this offset
                stream.Read(packbuf, 0, 2);

                if (packbuf[0] == 16 && packbuf[1] == 0xfb)
                {
                    this.isCompressed = true;
                    return new MemoryStream(QfsComp.Decomp(stream, 0, (int)stream.Length));
                }
            }
            byte[] buffer = new byte[stream.Length];
            stream.ProperRead(buffer, 0, buffer.Length);
            return new MemoryStream(buffer);
        }

        private static int GetBmpDataSize(int width, int height, int code)
        {
            int size = 0;
            switch (code)
            {
                case 0x7d:
                    size = (width * height) * 4;
                    break;
                case 0x7f:
                    size = (width * height) * 3;
                    break;
                case 0x60:
                    size = (width * height) / 2;
                    break;
                case 0x61:
                    size = (width * height);
                    break;
            }

            return size;
        }


        private static unsafe void ReadDxtImageData(byte[] rgba, ref BitmapEntry entry, Rectangle lockRect)
        {
            BitmapData bd = entry.Bitmap.LockBits(lockRect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            BitmapData ad = entry.Alpha.LockBits(lockRect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            int width = lockRect.Width;
            int height = lockRect.Height;

            try
            {
                byte* bmpScan0 = (byte*)bd.Scan0.ToPointer();
                byte* alScan0 = (byte*)ad.Scan0.ToPointer();
                int bmpStride = bd.Stride;
                int alStride = ad.Stride;

                int srcStride = width * 4;

                fixed (byte* ptr = rgba)
                {
                    for (int y = 0; y < height; y++)
                    {
                        byte* src = ptr + (y * srcStride);
                        byte* p = bmpScan0 + (y * bmpStride);
                        byte* q = alScan0 + (y * alStride);

                        for (int x = 0; x < width; x++)
                        {
                            p[0] = src[2];
                            p[1] = src[1];
                            p[2] = src[0];
                            q[0] = q[1] = q[2] = src[3];

                            p += 3;
                            q += 3;
                            src += 4;
                        }
                    }
                }

            }
            finally
            {
                entry.Bitmap.UnlockBits(bd);
                entry.Alpha.UnlockBits(ad);
            }
        }

        /// <summary>
        /// Loads a Fsh image from the specified stream.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <exception cref="System.FormatException">Thrown when the file is invalid.</exception>
        /// <exception cref="System.FormatException">Thrown when the header of the fsh file is invalid.</exception>
        /// <exception cref="System.FormatException">Thrown when the fsh file contains an unhandled image format.</exception>
        private unsafe void Load(Stream stream)
        {
            if (stream.Length <= 4)
            {
                throw new FormatException(Resources.InvalidFshFile);
            }

            stream.Position = 0L;
            MemoryStream ms = null;

            try
            {
                ms = Decompress(stream);
                this.rawData = ms.ToArray();
                using (BinaryReader br = new BinaryReader(ms))
                {
                    byte[] SHPI = br.ReadBytes(4);
                    if (Encoding.ASCII.GetString(SHPI) != "SHPI")
                    {
                        throw new FormatException(Resources.InvalidFshHeader);
                    }
                    this.header = new FSHHeader();
                    header.SHPI = SHPI;
                    header.size = br.ReadInt32();
                    header.numBmps = br.ReadInt32();
                    header.dirID = br.ReadBytes(4);

                    int fshSize = header.size;
                    int nbmp = header.numBmps;
                    this.dirs = new FSHDirEntry[nbmp];
                    this.bitmaps = new List<BitmapEntry>(nbmp);

                    for (int i = 0; i < nbmp; i++)
                    {
                        this.dirs[i].name = br.ReadBytes(4);
                        this.dirs[i].offset = br.ReadInt32();
                    }

                    for (int i = 0; i < nbmp; i++)
                    {
                        br.BaseStream.Seek((long)this.dirs[i].offset, SeekOrigin.Begin);
                        FSHEntryHeader eHeader = new FSHEntryHeader();
                        eHeader.code = br.ReadInt32();
                        eHeader.width = br.ReadInt16();
                        eHeader.height = br.ReadInt16();
                        eHeader.misc = new short[4];
                        for (int m = 0; m < 4; m++)
                        {
                            eHeader.misc[m] = br.ReadInt16();
                        }

                        int code = (eHeader.code & 0x7f);

                        if ((code == 0x7b) ||  (code == 0x7e) || (code == 0x78) || (code == 0x6d))
                        {
                            throw new FormatException(Resources.UnsupportedFshType); // bail on the non SC4 formats
                        }

                        bool isBmp = ((code == 0x7d) || (code == 0x7f) || (code == 0x60) || (code == 0x61));

                        int width = (int)eHeader.width;
                        int height = (int)eHeader.height;
                        int nAttach = 0;
                        int auxofs = 0;

                        if (isBmp)
                        {
                            FSHEntryHeader aux = eHeader;
                            nAttach =  0;
                            auxofs = dirs[i].offset;
                            while ((aux.code >> 8) > 0) 
                            {                      
                                auxofs += (aux.code >> 8);

                                if ((auxofs + 16) >= fshSize)
                                {
                                    break;
                                }
                                nAttach++;
                               
                                br.BaseStream.Seek(auxofs, SeekOrigin.Begin);
                                aux = new FSHEntryHeader();
                                aux.code = br.ReadInt32();
                                aux.width = br.ReadInt16();
                                aux.height = br.ReadInt16();
                                aux.misc = new short[4];
                                for (int m = 0; m < 4; m++)
                                {
                                    aux.misc[m] = br.ReadInt16();
                                }
                            }

                            BitmapEntry entry = new BitmapEntry() { BmpType = (FSHBmpType)code, DirName = Encoding.ASCII.GetString(this.dirs[i].name) };
                            entry.Bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
                            entry.Alpha = new Bitmap(width, height, PixelFormat.Format24bppRgb);

                            int dataSize = GetBmpDataSize(width, height, code);
                            byte[] data = br.ReadBytes(dataSize);
                            Rectangle lockRect = new Rectangle(0, 0, width, height);
                            BitmapData bd = null;
                            BitmapData ad = null;
                            byte* bmpScan0 = null;
                            byte* alScan0 = null;
                            int bmpStride = 0;
                            int alStride = 0;

                            int srcStride = 0;

                            if (code == 0x7d) // 32-bit BGRA
                            {

                                bd = entry.Bitmap.LockBits(lockRect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
                                ad = entry.Alpha.LockBits(lockRect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

                                try
                                {
                                    bmpScan0 = (byte*)bd.Scan0.ToPointer();
                                    alScan0 = (byte*)ad.Scan0.ToPointer();
                                    bmpStride = bd.Stride;
                                    alStride = ad.Stride;
                                    srcStride = width * 4;

                                    fixed (byte* ptr = data)
                                    {
                                        for (int y = 0; y < height; y++)
                                        {
                                            byte* src = ptr + (y * srcStride);
                                            byte* p = bmpScan0 + (y * bmpStride);
                                            byte* q = alScan0 + (y * alStride);

                                            for (int x = 0; x < width; x++)
                                            {
                                                p[0] = src[0];
                                                p[1] = src[1];
                                                p[2] = src[2];
                                                q[0] = q[1] = q[2] = src[3];

                                                p += 3;
                                                q += 3;
                                                src += 4;
                                            }
                                        }
                                    }

                                }
                                finally
                                {
                                    entry.Bitmap.UnlockBits(bd);
                                    entry.Alpha.UnlockBits(ad);
                                }
                            }
                            else if (code == 0x7f) // 24-bit BGR
                            {
                                bd = entry.Bitmap.LockBits(lockRect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
                                ad = entry.Alpha.LockBits(lockRect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

                                try
                                {
                                    bmpScan0 = (byte*)bd.Scan0.ToPointer();
                                    alScan0 = (byte*)ad.Scan0.ToPointer();
                                    bmpStride = bd.Stride;
                                    alStride = ad.Stride;

                                    srcStride = width * 3;

                                    fixed (byte* ptr = data)
                                    {
                                        for (int y = 0; y < height; y++)
                                        {
                                            byte* src = ptr + (y * srcStride);
                                            byte* p = bmpScan0 + (y * bmpStride);
                                            byte* q = alScan0 + (y * alStride);

                                            for (int x = 0; x < width; x++)
                                            {
                                                p[0] = src[0];
                                                p[1] = src[1];
                                                p[2] = src[2];
                                                q[0] = q[1] = q[2] = 255;

                                                p += 3;
                                                q += 3;
                                                src += 3;
                                            }
                                        }
                                    }

                                }
                                finally
                                {
                                    entry.Bitmap.UnlockBits(bd);
                                    entry.Alpha.UnlockBits(ad);
                                }
                            }
                            else if (code == 0x60 || code == 0x61) // DXT1 or DXT3
                            {
                                SquishFlags flag = code == 0x60 ? SquishFlags.kDxt1 : SquishFlags.kDxt3;
                                byte[] rgba = Squish.DecompressImage(data, width, height, (int)flag);

                                ReadDxtImageData(rgba, ref entry, lockRect);
                            }
                           
                            this.bitmaps.Add(entry);
                        }

                    }

                }
                ms = null;
            }
            finally
            {
                if (ms != null)
                {
                    ms.Dispose();
                    ms = null;
                }
            }
            
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FSHImageWrapper"/> class.
        /// </summary>
        public FSHImageWrapper()
        {
            this.bitmaps = new List<BitmapEntry>();
            this.dirs = null;
            this.isCompressed = false;
            this.rawData = null;
            this.disposed = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FSHImageWrapper"/> class from the specified stream.
        /// </summary>
        /// <param name="stream">The stream to load from.</param>
        public FSHImageWrapper(Stream stream)
        {
            this.Load(stream);
        }

        private FSHImageWrapper(FSHImageWrapper cloneMe)
        {
            this.header = cloneMe.header;
            this.bitmaps = new List<BitmapEntry>(cloneMe.bitmaps.Count);

            for (int i = 0; i < cloneMe.bitmaps.Count; i++)
            {
                BitmapEntry entry = cloneMe.bitmaps[i].Clone();
                this.bitmaps.Add(entry);
            }

            this.dirs = new FSHDirEntry[cloneMe.dirs.Length];
            cloneMe.dirs.CopyTo(this.dirs, 0);
            this.isCompressed = cloneMe.isCompressed;
            this.rawData = new byte[cloneMe.rawData.Length];
            Buffer.BlockCopy(cloneMe.rawData, 0, this.rawData, 0, this.rawData.Length);
        }

        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns></returns>
        public FSHImageWrapper Clone()
        {
            return new FSHImageWrapper(this);
        }

        /// <summary>
        /// Gets the directory entry for the specified index.
        /// </summary>
        /// <param name="index">The index of the entry.</param>
        /// <returns>The FSHDirEntry at the specified index.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">The index is less than zero or greater than the number of entries.</exception>
        public FSHDirEntry GetDirectoryEntry(int index)
        {
            if (index < 0 || index > (this.dirs.Length - 1))
            {
                throw new ArgumentOutOfRangeException("index");
            }

            return this.dirs[index];
        }

        /// <summary>
        /// Gets the entry header from the image.
        /// </summary>
        /// <param name="offset">The offset of hte start of the header.</param>
        /// <returns></returns>
        public FSHEntryHeader GetEntryHeader(int offset)
        {
            FSHEntryHeader entry = new FSHEntryHeader();

            if (rawData != null && (offset + 16) < rawData.Length)
            {
                entry.code = BitConverter.ToInt32(rawData, offset);
                entry.width = BitConverter.ToInt16(rawData, offset + 4);
                entry.height = BitConverter.ToInt16(rawData, offset + 6);
                entry.misc = new short[4];
                Array.Copy(rawData, offset + 8, entry.misc, 0, 4);
            }

            return entry;
        }

        /// <summary>
        /// Saves the FSHImage to the specified stream.
        /// </summary>
        /// <param name="input">The input.</param>
        public void Save(Stream input)
        {
            FSHImage image = new FSHImage() { IsCompressed = this.isCompressed };
            BitmapItem[] items = new BitmapItem[this.bitmaps.Count];
            for (int i = 0; i < this.bitmaps.Count; i++)
            {
                items[i] = this.bitmaps[i].ToBitmapItem();
            }

            image.Bitmaps.AddRange(items);

            image.Save(input);
        }

        /// <summary>
        /// Creates a new FSHImage from the <see cref="FSHImageWrapper"/> instance.
        /// </summary>
        /// <returns>A new FSHImage.</returns>
        public FSHImage ToFSHImage()
        {
            FSHImage image = new FSHImage();
            BitmapItem[] items = new BitmapItem[this.bitmaps.Count];
            for (int i = 0; i < this.bitmaps.Count; i++)
            {
                items[i] = this.bitmaps[i].ToBitmapItem();
            }

            image.Bitmaps.AddRange(items);
            image.SetDirectories(this.dirs);
            image.SetRawData(this.rawData);

            return image; 
        }

        /// <summary>
        /// Creates a new <see cref="FSHImageWrapper"/> from the specified FSHImage.
        /// </summary>
        /// <param name="fsh">The FSHImage to copy from.</param>
        /// <returns>The new <see cref="FSHImageWrapper"/> for the specified FSHImage.</returns>
        /// <exception cref="System.ArgumentNullException">The specified FSHImage is null.</exception>
        public static FSHImageWrapper FromFSHImage(FSHImage fsh)
        {
            if (fsh == null)
            {
                throw new ArgumentNullException("fsh");
            }

            FSHImageWrapper wrap = new FSHImageWrapper();
            wrap.bitmaps = new List<BitmapEntry>(fsh.Bitmaps.Count);

            for (int i = 0; i < fsh.Bitmaps.Count; i++)
            {
                wrap.bitmaps.Add((BitmapItem)fsh.Bitmaps[i]);
            }
            wrap.dirs = new FSHDirEntry[fsh.Directory.Length];
            fsh.Directory.CopyTo(wrap.dirs, 0);
            wrap.header = fsh.Header;
            wrap.rawData = fsh.RawData;

            return wrap;
        }


        private bool disposed;
        public void Dispose()
        {
            if (!disposed)
            {
                if (bitmaps != null)
                {
                    foreach (var item in bitmaps)
                    {
                        item.Dispose();
                    }
                    bitmaps.Clear();
                    bitmaps = null;
                }

                this.disposed = true;
            }

            GC.SuppressFinalize(this);
        }

       
    }
}
