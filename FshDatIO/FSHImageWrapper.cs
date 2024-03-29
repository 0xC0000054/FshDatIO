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

using FshDatIO.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Security.Permissions;

namespace FshDatIO
{
    /// <summary>
    /// The class that encapsulates a fsh image.
    /// </summary>
    public sealed class FSHImageWrapper : IDisposable
    {
        private FSHHeader header;
        private BitmapEntryCollection bitmaps;
        private FSHDirEntry[] dirs;
        private bool isCompressed;
        private byte[] rawData;
        private bool disposed;

        /// <summary>
        /// Gets the collection of <see cref="BitmapEntry"/> items.
        /// </summary>
        public BitmapEntryCollection Bitmaps
        {
            get
            {
                return this.bitmaps;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is compressed.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is compressed; otherwise, <c>false</c>.
        /// </value>
        public bool IsCompressed
        {
            get
            {
                return this.isCompressed;
            }
            set
            {
                this.isCompressed = value;
            }
        }

        /// <summary>
        /// Gets the FSH image header.
        /// </summary>
        public FSHHeader Header
        {
            get
            {
                return this.header;
            }
        }

        internal uint RawDataLength
        {
            get
            {
                return (uint)this.rawData.Length;
            }
        }

        private byte[] Decompress(byte[] imageBytes)
        {
            if ((imageBytes[0] & 0xfe) == 0x10 && imageBytes[1] == 0xFB || imageBytes[4] == 0x10 && imageBytes[5] == 0xFB)
            {
                this.isCompressed = true;
                return QfsCompression.Decompress(imageBytes);
            }

            return imageBytes;
        }

        /// <summary>
        /// Gets the size of the BMP data.
        /// </summary>
        /// <param name="width">The width if the image.</param>
        /// <param name="height">The height of the image.</param>
        /// <param name="code">The bitmap code of the image.</param>
        /// <returns>The size of the bitmap data.</returns>
        private static int GetBmpDataSize(int width, int height, FshImageFormat code)
        {
            int size;
            switch (code)
            {
                case FshImageFormat.ThirtyTwoBit:
                    size = (width * height) * 4;
                    break;
                case FshImageFormat.TwentyFourBit:
                    size = (width * height) * 3;
                    break;
                case FshImageFormat.DXT1:
                    size = (width * height) / 2;
                    break;
                case FshImageFormat.DXT3:
                    size = (width * height);
                    break;
                default:
                    throw new FormatException(Resources.UnsupportedFshType);
            }

            return size;
        }

        private static unsafe void DecodeImageData(byte[] imageData, int startOffset, int width, int height, ref BitmapEntry entry)
        {
            entry.Bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            entry.Alpha = new Bitmap(width, height, PixelFormat.Format24bppRgb);

            FshImageFormat format = entry.BmpType;
            Rectangle lockRect = new Rectangle(0, 0, width, height);
            BitmapData bd = null;
            BitmapData ad = null;
            byte* bmpScan0 = null;
            byte* alScan0 = null;
            int bmpStride = 0;
            int alStride = 0;

            int srcStride = 0;

            if (format == FshImageFormat.DXT1 || format == FshImageFormat.DXT3)
            {
                byte[] rgba = DXTComp.UnpackDXTImage(imageData, startOffset, width, height, (format == FshImageFormat.DXT1));

                bd = entry.Bitmap.LockBits(lockRect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
                ad = entry.Alpha.LockBits(lockRect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

                try
                {
                    bmpScan0 = (byte*)bd.Scan0.ToPointer();
                    alScan0 = (byte*)ad.Scan0.ToPointer();
                    bmpStride = bd.Stride;
                    alStride = ad.Stride;

                    srcStride = width * 4;

                    fixed (byte* ptr = rgba)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            byte* src = ptr + (y * srcStride);
                            byte* dst = bmpScan0 + (y * bmpStride);
                            byte* alpha = alScan0 + (y * alStride);

                            for (int x = 0; x < width; x++)
                            {
                                dst[0] = src[2];
                                dst[1] = src[1];
                                dst[2] = src[0];
                                alpha[0] = alpha[1] = alpha[2] = src[3];

                                dst += 3;
                                alpha += 3;
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
            else if (format == FshImageFormat.ThirtyTwoBit) // 32-bit BGRA
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

                    fixed (byte* ptr = &imageData[startOffset])
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
            else if (format == FshImageFormat.TwentyFourBit) // 24-bit BGR
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

                    fixed (byte* ptr = &imageData[startOffset])
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
        }

        private System.Collections.ObjectModel.ReadOnlyCollection<FshAttachment> ParseAttachments(int auxHeaderCode, int dirOffset, int nextOffset, int fshSize, int count)
        {
            List<FshAttachment> attachments = new List<FshAttachment>(count);
            int auxOffset = dirOffset;
            EntryHeader auxHeader = new EntryHeader(auxHeaderCode);

            for (int j = 0; j < count; j++)
            {
                auxOffset += (auxHeader.Code >> 8);

                if ((auxOffset + 4) >= fshSize)
                {
                    break;
                }

                auxHeader.Code = LittleEndianBitConverter.ToInt32(this.rawData, auxOffset);
                auxOffset += 4;

                int attachCode = auxHeader.Code & 0xff;

                if (attachCode == 0x22 || attachCode == 0x24 || attachCode == 0x29 || attachCode == 0x2a || attachCode == 0x2d)
                {
                    continue; // Skip any Indexed color palettes.
                }

                if (attachCode == 0x6f || attachCode == 0x69 || attachCode == 0x7c)
                {
                    try
                    {
                        auxHeader.Width = LittleEndianBitConverter.ToUInt16(this.rawData, auxOffset);
                        auxOffset += 2;
                        auxHeader.Height = LittleEndianBitConverter.ToUInt16(this.rawData, auxOffset);
                        auxOffset += 2;
                        if (attachCode == 0x69 || attachCode == 0x7c)
                        {
                            ushort[] misc = new ushort[4];
                            for (int i = 0; i < 4; i++)
                            {
                                misc[i] = LittleEndianBitConverter.ToUInt16(this.rawData, auxOffset);
                                auxOffset += 2;
                            }
                            auxHeader.SetMiscData(misc);
                        }
                    }
                    catch (EndOfStreamException)
                    {
                        break;
                    }
                }

                byte[] attachBytes = null;
                int dataLength = 0;
                switch (attachCode)
                {
                    case 0x6f: // TXT                                    
                    case 0x69: // ETXT full header
                        attachBytes = new byte[auxHeader.Width];
                        Buffer.BlockCopy(this.rawData, auxOffset, attachBytes, 0, attachBytes.Length);
                        break;
                    case 0x70: // ETXT 16 bytes including code length
                        attachBytes = new byte[12];
                        Buffer.BlockCopy(this.rawData, auxOffset, attachBytes, 0, attachBytes.Length);
                        break;
                    case 0x7c: // Pixel region
                    default: // Binary data
                        dataLength = auxHeader.Code >> 8;
                        if (dataLength == 0)
                        {
                            dataLength = nextOffset - auxOffset;
                        }
                        if (dataLength > 16384)
                        {
                            // attachment data too large skip it
                            continue;
                        }

                        attachBytes = new byte[dataLength];
                        Buffer.BlockCopy(this.rawData, auxOffset, attachBytes, 0, attachBytes.Length);
                        break;
                }

                attachments.Add(new FshAttachment(auxHeader, attachBytes));
            }

            return attachments.AsReadOnly();
        }

        /// <summary>
        /// Loads a Fsh image from the specified byte array.
        /// </summary>
        /// <param name="imageBytes">The byte array containing the image data.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when the byte array is null.</exception>
        /// <exception cref="System.FormatException">Thrown when the file is invalid.</exception>
        /// <exception cref="System.FormatException">Thrown when the file is invalid.</exception>
        /// <exception cref="System.FormatException">Thrown when the file is invalid.</exception>
        private unsafe void Load(byte[] imageBytes)
        {
            if (imageBytes == null)
            {
                throw new ArgumentNullException("imageBytes");
            }

            if (imageBytes.Length <= 4)
            {
                throw new FormatException(Resources.InvalidFshFile);
            }

            this.rawData = Decompress(imageBytes);
            this.header = new FSHHeader(this.rawData);

            int fshSize = header.Size;
            int imageCount = header.ImageCount;

            this.dirs = new FSHDirEntry[imageCount];
            this.bitmaps = new BitmapEntryCollection(imageCount);

            int directoryOffset = 16;
            for (int i = 0; i < imageCount; i++)
            {
                this.dirs[i] = new FSHDirEntry(this.rawData, directoryOffset);
                directoryOffset += FSHDirEntry.SizeOf;
            }

            int nextOffset = fshSize;
            for (int i = 0; i < imageCount; i++)
            {
                FSHDirEntry dir = this.dirs[i];

                for (int j = 0; j < imageCount; j++)
                {
                    if (dirs[j].Offset > dir.Offset && dirs[j].Offset < nextOffset)
                    {
                        nextOffset = dirs[j].Offset;
                    }
                }

                EntryHeader eHeader = new EntryHeader(this.rawData, dir.Offset);

                int code = eHeader.Code & 0x7f;

                if (code == 0x7b || code == 0x7e || code == 0x78 || code == 0x6d)
                {
                    throw new FormatException(Resources.UnsupportedFshType); // bail on the non SC4 formats
                }

                bool isBmp = (code == 0x7d || code == 0x7f || code == 0x60 || code == 0x61);

                if (isBmp)
                {
                    int bmpStartOffset = dir.Offset + EntryHeader.SizeOf;
                    int auxCode = eHeader.Code;
                    int attachCount = 0;
                    int auxOffset = dir.Offset;
                    while ((auxCode >> 8) > 0)
                    {
                        auxOffset += (auxCode >> 8); // the section length is the start offset for the attachment

                        if ((auxOffset + 4) >= nextOffset)
                        {
                            break;
                        }
                        attachCount++;

                        auxCode = LittleEndianBitConverter.ToInt32(this.rawData, auxOffset);
                    }
                    int width = eHeader.Width;
                    int height = eHeader.Height;
                    bool entryCompressed = (eHeader.Code & 0x80) != 0;

                    int numScales = 0;
                    bool packedMbp = false;
                    ushort[] miscData = eHeader.GetMiscDataReadOnly();

                    if (!entryCompressed && (miscData[3] & 0x0fff) == 0)
                    {
                        numScales = (miscData[3] >> 12) & 0x0f;

                        if ((width % (1 << numScales)) > 0 || (height % (1 << numScales)) > 0)
                        {
                            numScales = 0;
                        }

                        if (numScales > 0) // check for multiscale bitmaps
                        {
                            int mbpLen = 0;
                            int mbpPadLen = 0;

                            for (int j = 0; j <= numScales; j++)
                            {
                                int mipWidth = (width >> j);
                                int mipHeight = (height >> j);

                                int dataLength;
                                switch (code)
                                {
                                    case 0x60:
                                        // DXT1 images must be padded to a multiple of four.
                                        dataLength = ((((mipWidth + 3) & ~3) * ((mipHeight + 3) & ~3)) / 2);
                                        break;
                                    case 0x61:
                                        dataLength = (mipWidth * mipHeight);
                                        break;
                                    case 0x7d:
                                        dataLength = (mipWidth * mipHeight * 4);
                                        break;
                                    case 0x7f:
                                        dataLength = (mipWidth * mipHeight * 3);
                                        break;
                                    default:
                                        throw new FormatException(Resources.UnsupportedFshType);
                                }
                                mbpLen += dataLength;
                                mbpPadLen += dataLength;

                                // DXT1 mipmaps smaller than 4x4 are also padded
                                int padLen = ((16 - mbpLen) & 15);
                                if (padLen > 0)
                                {
                                    mbpLen += padLen;
                                    if (j == numScales)
                                    {
                                        mbpPadLen += ((16 - mbpPadLen) & 15);
                                    }
                                }
                            }

                            int imageLength = eHeader.Code >> 8;
                            if (imageLength != 0 && imageLength != (mbpLen + EntryHeader.SizeOf) ||
                                imageLength == 0 && (mbpLen + dir.Offset + EntryHeader.SizeOf) != nextOffset)
                            {
                                packedMbp = true;
                                if (imageLength != 0 && imageLength != (mbpPadLen + EntryHeader.SizeOf) ||
                                    imageLength == 0 && (mbpPadLen + dir.Offset + EntryHeader.SizeOf) != nextOffset)
                                {
                                    numScales = 0;
                                }
                            }
                        }
                    }

                    FshImageFormat format = (FshImageFormat)code;
                    BitmapEntry entry = new BitmapEntry(format, dir.Name, numScales, packedMbp, miscData);

                    if (entryCompressed)
                    {
                        int compSize = 0;

                        int sectionLength = eHeader.Code >> 8;
                        if (sectionLength > 0)
                        {
                            compSize = sectionLength;
                        }
                        else
                        {
                            compSize = nextOffset - bmpStartOffset;
                        }

                        byte[] compressedData = new byte[compSize];
                        Buffer.BlockCopy(this.rawData, bmpStartOffset, compressedData, 0, compSize);
                        
                        DecodeImageData(QfsCompression.Decompress(compressedData), 0, width, height, ref entry);
                    }
                    else
                    {
                        DecodeImageData(this.rawData, bmpStartOffset, width, height, ref entry);
                    }


                    if (attachCount > 0)
                    {
                        entry.Attachments = ParseAttachments(eHeader.Code, dir.Offset, nextOffset, fshSize, attachCount);
                    }

                    this.bitmaps.Add(entry);
                }
            }

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FSHImageWrapper"/> class.
        /// </summary>
        public FSHImageWrapper()
        {
            this.bitmaps = new BitmapEntryCollection();
            this.dirs = null;
            this.isCompressed = false;
            this.rawData = null;
            this.disposed = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FSHImageWrapper"/> class from the specified stream.
        /// </summary>
        /// <param name="stream">The stream to load from.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when the stream is null.</exception>
        /// <exception cref="System.FormatException">Thrown when the file is invalid.</exception>
        /// <exception cref="System.FormatException">Thrown when the header of the fsh file is invalid.</exception>
        /// <exception cref="System.FormatException">Thrown when the fsh file contains an unhandled image format.</exception>
        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
        public FSHImageWrapper(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }
            this.bitmaps = new BitmapEntryCollection();
            this.dirs = null;
            this.isCompressed = false;
            this.rawData = null;
            this.disposed = false;

            byte[] bytes = stream.ReadBytes((int)stream.Length);
            Load(bytes);
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="FSHImageWrapper"/> class.
        /// </summary>
        /// <param name="imageBytes">The image bytes.</param>
        /// <exception cref="System.ArgumentException">Thrown when the byte array is null or empty.</exception>
        /// <exception cref="System.FormatException">Thrown when the file is invalid.</exception>
        /// <exception cref="System.FormatException">Thrown when the header of the fsh file is invalid.</exception>
        /// <exception cref="System.FormatException">Thrown when the fsh file contains an unhandled image format.</exception>
        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
        internal FSHImageWrapper(byte[] imageBytes)
        {
            if (imageBytes == null)
            {
                throw new ArgumentNullException("imageBytes");
            }

            if (imageBytes.Length == 0)
            {
                throw new ArgumentException("imageBytes is a zero length array.", "imageBytes");
            }
            this.bitmaps = new BitmapEntryCollection();
            this.dirs = null;
            this.isCompressed = false;
            this.rawData = null;
            this.disposed = false;

            Load(imageBytes);
        }

        private FSHImageWrapper(FSHImageWrapper cloneMe)
        {
            this.header = cloneMe.header;
            this.bitmaps = new BitmapEntryCollection(cloneMe.bitmaps.Count);

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
        /// <param name="index">The index of the temp.</param>
        /// <returns> The FSHDirEntry at the specified index.</returns>
        /// <exception cref="System.InvalidOperationException">The image has not been loaded.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">The index is less than zero or greater than the number of entries.</exception>
        public FSHDirEntry GetDirectoryEntry(int index)
        {
            if (this.dirs == null)
            {
                throw new InvalidOperationException(Properties.Resources.ImageNotLoaded);
            }

            if (index < 0 || index > (this.dirs.Length - 1))
            {
                throw new ArgumentOutOfRangeException("index");
            }

            return this.dirs[index];
        }

        /// <summary>
        /// Gets the entry header from the image.
        /// </summary>
        /// <param name="offset">The offset of the start of the header.</param>
        /// <returns>The EntryHeader at the specified offset.</returns>
        /// <exception cref="System.InvalidOperationException">The image has not been loaded.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">The offset is less than zero or greater than the length of the file.</exception>
        public EntryHeader GetEntryHeader(int offset)
        {
            if (this.rawData == null)
            {
                throw new InvalidOperationException(Properties.Resources.ImageNotLoaded);
            }

            if (offset < 0 || offset > (this.rawData.Length - EntryHeader.SizeOf))
            {
                throw new ArgumentOutOfRangeException("offset");
            }

            return new EntryHeader(this.rawData, offset);
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

            if (color.PixelFormat == PixelFormat.Format32bppArgb)
            {
                return (Bitmap)color.Clone();
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

        private static unsafe byte[] EncodeImageData(Bitmap srcImage, FshImageFormat format, bool fshWriteCompression, int dataLength)
        {
            int width = srcImage.Width;
            int height = srcImage.Height;

            BitmapData bd = srcImage.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            byte* scan0 = (byte*)bd.Scan0.ToPointer();
            int stride = bd.Stride;

            byte[] data = null;

            if (format != FshImageFormat.DXT1 && format != FshImageFormat.DXT3)
            {
                data = new byte[dataLength + 2000];
            }

            try
            {

                if (format == FshImageFormat.TwentyFourBit)
                {
                    fixed (byte* ptr = data)
                    {
                        int dstStride = width * 3;

                        for (int y = 0; y < height; y++)
                        {
                            byte* src = scan0 + (y * stride);
                            byte* dst = ptr + (y * dstStride);
                            for (int x = 0; x < width; x++)
                            {
                                dst[0] = src[0];
                                dst[1] = src[1];
                                dst[2] = src[2];

                                src += 4;
                                dst += 3;
                            }
                        }
                    }
                }
                else if (format == FshImageFormat.ThirtyTwoBit)
                {
                    fixed (byte* ptr = data)
                    {
                        int dstStride = width * 4;

                        for (int y = 0; y < height; y++)
                        {
                            byte* src = scan0 + (y * stride);
                            byte* dst = ptr + (y * dstStride);
                            for (int x = 0; x < width; x++)
                            {
                                dst[0] = src[0];
                                dst[1] = src[1];
                                dst[2] = src[2];
                                dst[3] = src[3];

                                src += 4;
                                dst += 4;
                            }
                        }
                    }
                }
                else if (format == FshImageFormat.DXT1)
                {
                    if (fshWriteCompression)
                    {
                        int flags = 0;
                        flags |= (int)SquishFlags.kDxt1;
                        flags |= (int)SquishFlags.kColourIterativeClusterFit;
                        flags |= (int)SquishFlags.kColourMetricPerceptual;
                        data = Squish.CompressImage(scan0, stride, width, height, flags);
                    }
                    else
                    {
                        data = DXTComp.CompressFSHToolDXT1(scan0, width, height);
                    }
                }
                else if (format == FshImageFormat.DXT3)
                {
                    if (fshWriteCompression)
                    {
                        int flags = 0;
                        flags |= (int)SquishFlags.kDxt3;
                        flags |= (int)SquishFlags.kColourIterativeClusterFit;
                        flags |= (int)SquishFlags.kColourMetricPerceptual;
                        data = Squish.CompressImage(scan0, stride, width, height, flags);
                    }
                    else
                    {
                        data = DXTComp.CompressFSHToolDXT3(scan0, width, height);
                    }
                }
            }
            finally
            {
                srcImage.UnlockBits(bd);
            }

            return data;
        }

        private static void WriteMipMaps(Stream stream, Bitmap srcImage, BitmapEntry entry, bool fshWriteCompression)
        {
            Bitmap temp = null;

            try
            {
                int width = srcImage.Width;
                int height = srcImage.Height;
                FshImageFormat format = entry.BmpType;
                int mipCount = entry.EmbeddedMipmapCount;

                for (int j = 1; j <= mipCount; j++)
                {
                    int scaledWidth = (width >> j);
                    int scaledHeight = (height >> j);

                    if (temp != null)
                    {
                        temp.Dispose();
                        temp = null;
                    }

                    if (format == FshImageFormat.DXT1 && (scaledWidth < 4 || scaledHeight < 4))
                    {
                        // For DXT1 the mipmaps smaller then 4x4 are padded with transparent pixels
                        temp = new Bitmap(4, 4, PixelFormat.Format32bppArgb);

                        using (Bitmap image = SuperSample.GetBitmapThumbnail(srcImage, scaledWidth, scaledHeight))
                        using (Graphics g = Graphics.FromImage(temp))
                        {
                            g.Clear(Color.FromArgb(0, 0, 0, 0));
                            g.DrawImageUnscaled(image, 0, 0);
                        }
                        scaledWidth = 4;
                        scaledHeight = 4;
                    }
                    else
                    {
                        temp = SuperSample.GetBitmapThumbnail(srcImage, scaledWidth, scaledHeight);
                    }

                    int dataLength = GetBmpDataSize(scaledWidth, scaledHeight, format);
                    byte[] data = EncodeImageData(temp, format, fshWriteCompression, dataLength);

                    if (format != FshImageFormat.DXT1 && !entry.packedMbp || format == FshImageFormat.DXT1 && j == mipCount)
                    {
                        while ((dataLength & 15) > 0)
                        {
                            data[dataLength++] = 0; // pad to a 16 byte boundary
                        }
                    }

                    stream.Write(data, 0, dataLength);
                }
            }
            finally
            {
                if (temp != null)
                {
                    temp.Dispose();
                    temp = null;
                }
            }
        }

        /// <summary>
        /// Saves the FSHImageWrapper to the specified stream without FSH Write compression.
        /// </summary>
        /// <param name="output">The output stream to save to.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when the output stream is null.</exception>
        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
        public void Save(Stream output)
        {
            Save(output, false);
        }

        /// <summary>
        /// Saves the FSHImageWrapper to the specified stream.
        /// </summary>
        /// <param name="output">The output stream to save to.</param>
        /// <param name="fshWriteCompression">if set to <c>true</c> use FSH Write compression on DXT1 or DXT3 images.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when the output stream is null.</exception>
        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
        public unsafe void Save(Stream output, bool fshWriteCompression)
        {
            if (output == null)
            {
                throw new ArgumentNullException("output");
            }

            using (MemoryStream stream = new MemoryStream())
            {
                int bitmapCount = this.bitmaps.Count;

                //write header
                FSHHeader newHeader = new FSHHeader(bitmapCount, "G264");
                newHeader.Save(stream);

                long directoryStart = stream.Position;
                FSHDirEntry[] directories = new FSHDirEntry[bitmapCount];

                for (int i = 0; i < bitmapCount; i++)
                {
                    directories[i] = new FSHDirEntry(this.bitmaps[i].DirName);
                    directories[i].Save(stream);
                }

                for (int i = 0; i < bitmapCount; i++)
                {
                    BitmapEntry entry = this.bitmaps[i];
                    FshImageFormat format = entry.BmpType;

                    long entryStart = stream.Position;
                    directories[i].Offset = (int)entryStart;

                    // write entry header

                    int mipCount = entry.EmbeddedMipmapCount;
                    ushort[] misc = entry.miscHeader;
                    if (misc == null)
                    {
                        misc = new ushort[4] { 0, 0, 0, (ushort)(mipCount << 12) };
                    }
                    Bitmap bmp = entry.Bitmap;
                    int width = bmp.Width;
                    int height = bmp.Height;

                    EntryHeader eHeader = new EntryHeader(format, width, height, misc);
                    eHeader.Save(stream);

                    Bitmap srcImage = null;

                    try
                    {
                        if (format == FshImageFormat.DXT1 && (width < 4 || height < 4))
                        {
                            srcImage = new Bitmap(4, 4, PixelFormat.Format32bppArgb);
                            using (Graphics gr = Graphics.FromImage(srcImage))
                            {
                                gr.DrawImageUnscaled(BlendDXTBitmap(bmp, entry.Alpha), 0, 0);
                            }
                        }
                        else
                        {
                            srcImage = BlendDXTBitmap(bmp, entry.Alpha);
                        }

                        int dataLength = GetBmpDataSize(srcImage.Width, srcImage.Height, format);
                        stream.Write(EncodeImageData(srcImage, format, fshWriteCompression, dataLength), 0, dataLength);

                        if (mipCount > 0)
                        {
                            WriteMipMaps(stream, srcImage, entry, fshWriteCompression);
                        }
                    }
                    finally
                    {
                        if (srcImage != null)
                        {
                            srcImage.Dispose();
                            srcImage = null;
                        }
                    }

                    // Write the section length if the entry has mipmaps, is compressed or has attachments.
                    if (mipCount > 0 || entry.Attachments != null)
                    {
                        long newPosition = stream.Position;
                        long sectionLength = newPosition - entryStart;
                        int newCode = (((int)sectionLength << 8) | eHeader.Code);
                        eHeader.Code = newCode;

                        stream.Seek(entryStart, SeekOrigin.Begin);
                        eHeader.Save(stream);
                        stream.Seek(newPosition, SeekOrigin.Begin);
                    }

                    if (entry.Attachments != null)
                    {
                        foreach (FshAttachment item in entry.Attachments)
                        {
                            stream.WriteInt32(item.Header.Code);

                            int attachCode = item.Header.Code & 0xff;

                            if (attachCode == 0x6f || attachCode == 0x69 || attachCode == 0x7c)
                            {
                                stream.WriteUInt16(item.Header.Width);
                                stream.WriteUInt16(item.Header.Height);

                                if (attachCode == 0x69 || attachCode == 0x7c)
                                {
                                    ushort[] miscData = item.Header.GetMiscDataReadOnly();
                                    for (int m = 0; m < 4; m++)
                                    {
                                        stream.WriteUInt16(miscData[m]);
                                    }
                                }
                            }

                            byte[] data = item.GetData();

                            if ((data != null) && data.Length > 0)
                            {
                                stream.Write(data, 0, data.Length);
                            }
                        }
                    }
                }

                newHeader.Size = (int)stream.Length;
                stream.Position = 0L;
                newHeader.Save(stream);

                stream.Position = directoryStart;
                for (int i = 0; i < directories.Length; i++)
                {
                    directories[i].Save(stream);
                }

                this.rawData = stream.ToArray();
                if (this.isCompressed)
                {
                    byte[] compbuf = null;

                    try
                    {
                        compbuf = QfsCompression.Compress(this.rawData, true);
                    }
                    catch (FormatException)
                    {
                    } 

                    if (compbuf != null)
                    {
                        output.Write(compbuf, 0, compbuf.Length);
                    }
                    else
                    {
                        this.isCompressed = false;
                        output.Write(this.rawData, 0, this.rawData.Length);
                    }
                }
                else
                {
                    stream.WriteTo(output);
                }
            }

        }

        /// <summary>
        /// Checks the size of the images within the fsh.
        /// </summary>
        /// <param name="imageBytes">The stream to read from.</param>
        /// <returns>True if all the image are at least 128x128; otherwise false.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown when the stream is null.</exception>
        internal static bool CheckImageSize(byte[] imageBytes)
        {
            if (imageBytes == null)
            {
                throw new ArgumentNullException("imageBytes");
            }

            if (imageBytes.Length <= 4)
            {
                throw new FormatException(Resources.InvalidFshFile);
            }


            byte[] rawData = null;
            if ((imageBytes[0] & 0xfe) == 0x10 && imageBytes[1] == 0xFB || imageBytes[4] == 0x10 && imageBytes[5] == 0xFB)
            {
                rawData = QfsCompression.Decompress(imageBytes);
            }
            else
            {
                rawData = imageBytes;
            }


            FSHHeader header = new FSHHeader(rawData);

            int nBmp = header.ImageCount;
            FSHDirEntry[] dirs = new FSHDirEntry[nBmp];

            int directoryOffset = 16;

            for (int i = 0; i < nBmp; i++)
            {
                dirs[i] = new FSHDirEntry(rawData, directoryOffset);
                directoryOffset += FSHDirEntry.SizeOf;
            }

            for (int i = 0; i < nBmp; i++)
            {
                FSHDirEntry dir = dirs[i];
                EntryHeader eHeader = new EntryHeader(rawData, dir.Offset);

                int code = eHeader.Code & 0x7f;

                if (code == 0x7b || code == 0x7e || code == 0x78 || code == 0x6d)
                {
                    throw new FormatException(Resources.UnsupportedFshType); // bail on the non SC4 formats
                }

                bool isBmp = (code == 0x7d || code == 0x7f || code == 0x60 || code == 0x61);

                if (isBmp && eHeader.Width < 128 && eHeader.Height < 128)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (!this.disposed)
            {
                this.disposed = true;
                if (this.bitmaps != null)
                {
                    this.bitmaps.Dispose();
                    this.bitmaps = null;
                }
            }
        }


    }
}
