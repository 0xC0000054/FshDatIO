using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Security.Permissions;
using System.Text;
using FshDatIO.Properties;

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

		/// <summary>
		/// Gets the collection of <see cref="BitmapEntry"/> items.
		/// </summary>
		public BitmapEntryCollection Bitmaps
		{
			get
			{
				return bitmaps;
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
				return isCompressed;
			}
			set
			{
				isCompressed = value;
			}
		}

		/// <summary>
		/// Gets the raw data.
		/// </summary>
		public byte[] GetRawData()
		{
			return rawData;
		}
		internal uint RawDataLength
		{ 
			get
			{
				return (uint)rawData.Length;
			}
		}

		private byte[] Decompress(byte[] imageBytes)
		{

			if ((imageBytes[0] & 0xfe) == 16 && imageBytes[1] == 0xfb) // NFS 1 uses this offset
			{
				this.isCompressed = true;
				return QfsComp.Decomp(imageBytes);
			}
            else if ((imageBytes[4] & 0xfe) == 16 && imageBytes[5] == 0xfb)
            {
                this.isCompressed = true;
                return QfsComp.Decomp(imageBytes);
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
			int size = 0;
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
		/// <param name="imageBytes">The byte array containing the image data.</param>
		/// <exception cref="System.ArgumentNullException">Thrown when the byte array is null.</exception>
		/// <exception cref="System.FormatException">Thrown when the file is invalid.</exception>
		/// <exception cref="System.FormatException">Thrown when the header of the fsh file is invalid.</exception>
		/// <exception cref="System.FormatException">Thrown when the fsh file contains an unhandled image format.</exception>
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

			MemoryStream stream = null;

			try
			{
				this.rawData = Decompress(imageBytes);
                stream = new MemoryStream(this.rawData);
				using (BinaryReader br = new BinaryReader(stream))
				{
                    stream = null;

					byte[] SHPI = br.ReadBytes(4);
					if (Encoding.ASCII.GetString(SHPI) != "SHPI")
					{
						throw new FormatException(Resources.InvalidFshHeader);
					}
					this.header = new FSHHeader();
					header.SHPI = SHPI;
					header.size = br.ReadInt32();
					header.imageCount = br.ReadInt32();
					header.dirID = br.ReadBytes(4);

					int fshSize = header.size;
					int nbmp = header.imageCount;
					this.dirs = new FSHDirEntry[nbmp];
					this.bitmaps = new BitmapEntryCollection(nbmp);

					for (int i = 0; i < nbmp; i++)
					{
						this.dirs[i].name = br.ReadBytes(4);
						this.dirs[i].offset = br.ReadInt32();
					}

					for (int i = 0; i < nbmp; i++)
					{
						FSHDirEntry dir = this.dirs[i];
						br.BaseStream.Seek((long)dir.offset, SeekOrigin.Begin);
						EntryHeader eHeader = new EntryHeader(br);

						int code = (eHeader.Code & 0x7f);

						if ((code == 0x7b) ||  (code == 0x7e) || (code == 0x78) || (code == 0x6d))
						{
							throw new FormatException(Resources.UnsupportedFshType); // bail on the non SC4 formats
						}

						bool isBmp = ((code == 0x7d) || (code == 0x7f) || (code == 0x60) || (code == 0x61));
                        bool entryCompressed = (eHeader.Code & 0x80) > 0; 


						int width = (int)eHeader.Width;
						int height = (int)eHeader.Height;
						int nAttach = 0;
						int auxofs = 0;

						if (isBmp)
						{
							long bmpStartOffset = (long)dir.offset + 16L;
							EntryHeader aux = eHeader;
							nAttach =  0;
							auxofs = dirs[i].offset;
							while ((aux.Code >> 8) > 0) 
							{                      
								auxofs += (aux.Code >> 8); // the section length is the start offset for the attachment

								if ((auxofs + 4) >= fshSize)
								{
									break;
								}
								nAttach++;

								br.BaseStream.Seek(auxofs, SeekOrigin.Begin);
								aux.Code = br.ReadInt32();
							}

                            int numScales= 0;
                            bool packedMbp = false;
                            if ((eHeader.Misc[3] & 0x0fff) == 0 && !entryCompressed)
                            {
                                numScales = (eHeader.Misc[3] >> 12) & 0x0f;

                                if ((width % (1 << numScales)) > 0 || (height % (1 << numScales)) > 0)
                                {
                                    numScales = 0;
                                }

                                if (numScales > 0) // check for multiscale bitmaps
                                {
                                    int bpp = 0;
                                    int mbpLen = 0;
                                    int mbpPadLen = 0;
                                    switch (code)
                                    {
                                        case 0x7b:
                                        case 0x61:
                                            bpp = 2;
                                            break;
                                        case 0x7d:
                                            bpp = 8;
                                            break;
                                        case 0x7f:
                                            bpp = 6;
                                            break;
                                        case 0x60:
                                            bpp = 1;
                                            break;
                                        default:
                                            bpp = 4;
                                            break;
                                    }

                                    int bmpw, bmph;

                                    for (int j = 0; j <= numScales; j++)
                                    {
                                        bmpw = (width >> j);
                                        bmph = (height >> j);


                                        if (code == 0x60)
                                        {
                                            bmpw += (4 - bmpw) & 3;
                                            bmph += (4 - bmph) & 3;
                                        }

                                        int length = (bmpw * bmph) * bpp / 2;
                                        mbpLen += length;
                                        mbpPadLen += length;
                                        int padLen = ((16 - mbpLen) & 15);
                                        if (padLen > 0)
                                        {
                                            mbpLen += padLen; // padding
                                            if (j == numScales)
                                            {
                                                mbpPadLen += ((16 - mbpPadLen) & 15);
                                            }
                                        }

#if DEBUG
										string sz = string.Format("{0}x{1} ({2})", bmpw, bmph, j);
										System.Diagnostics.Debug.WriteLine(string.Format("size: {0}, pad: {1}, length: {2}, {3}, padding: {4}",
											new object[] { sz, padLen.ToString(), length.ToString(), mbpLen.ToString(), mbpPadLen.ToString() }));
#endif
                                    }

                                    int imageLength = (eHeader.Code >> 8);
                                    if ((imageLength != mbpLen + 16) && (imageLength != 0) ||
                                        (imageLength == 0) && ((mbpLen + dir.offset + 16) != fshSize))
                                    {
                                        packedMbp = true;
                                        if ((imageLength != mbpPadLen + 16) && (imageLength != 0) ||
                                        (imageLength == 0) && ((mbpPadLen + dir.offset + 16) != fshSize))
                                        {
                                            numScales = 0;
                                        }
                                    }
                                }

                            }

							if (br.BaseStream.Position != bmpStartOffset)
							{
								br.BaseStream.Seek(bmpStartOffset, SeekOrigin.Begin);
							}

                            FshImageFormat format = (FshImageFormat)code;
							BitmapEntry entry = new BitmapEntry() { 
                                BmpType = format, 
                                DirName = Encoding.ASCII.GetString(dir.name),
                                EmbeddMipmapCount = numScales,
                                packedMbp = packedMbp,
                                miscHeader = eHeader.Misc
                            };
							entry.Bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
							entry.Alpha = new Bitmap(width, height, PixelFormat.Format24bppRgb);

							int dataSize = GetBmpDataSize(width, height, format);
							byte[] data = br.ReadBytes(dataSize);
							Rectangle lockRect = new Rectangle(0, 0, width, height);
							BitmapData bd = null;
							BitmapData ad = null;
							byte* bmpScan0 = null;
							byte* alScan0 = null;
							int bmpStride = 0;
							int alStride = 0;

							int srcStride = 0;

							if (format == FshImageFormat.ThirtyTwoBit) // 32-bit BGRA
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
							else if (format == FshImageFormat.DXT1 || format == FshImageFormat.DXT3) // DXT1 or DXT3
							{
								byte[] rgba = DXTComp.UnpackDXTImage(data, width, height, (format == FshImageFormat.DXT1));

								ReadDxtImageData(rgba, ref entry, lockRect);
							}
						   
							this.bitmaps.Add(entry);
						}

					}

				}
				stream = null;
			}
			finally
			{
				if (stream != null)
				{
					stream.Dispose();
					stream = null;
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
				throw new ArgumentNullException("stream");
			
			byte[] bytes = new byte[stream.Length];
			stream.ProperRead(bytes, 0, (int)stream.Length);
			this.Load(bytes);
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
		public FSHImageWrapper(byte[] imageBytes)
		{
			if (imageBytes == null || imageBytes.Length == 0)
				throw new ArgumentException("imageBytes is null or empty.", "imageBytes");

			this.Load(imageBytes);
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
		/// <param name="offset">The offset of the start of the header.</param>
		/// <returns></returns>
		/// <exception cref="System.ArgumentOutOfRangeException">The offset is less than zero or greater than the length of the file.</exception>
		public EntryHeader GetEntryHeader(int offset)
		{
			if (offset < 0 || (offset + 16) >= rawData.Length)
			{
				throw new ArgumentOutOfRangeException("offset");
			}

			EntryHeader entry = new EntryHeader();

			if (rawData != null)
			{
				entry.Code = BitConverter.ToInt32(rawData, offset);
				entry.Width = BitConverter.ToUInt16(rawData, offset + 4);
				entry.Height = BitConverter.ToUInt16(rawData, offset + 6);
				entry.Misc = new ushort[4];
				Array.Copy(rawData, offset + 8, entry.Misc, 0, 4);
			}

			return entry;
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

            using (MemoryStream ms = new MemoryStream())
            {
                int bitmapCount = this.bitmaps.Count;
                Encoding ascii = Encoding.ASCII;

                //write header
                ms.Write(ascii.GetBytes("SHPI"), 0, 4); // write SHPI id
                ms.Write(BitConverter.GetBytes(0), 0, 4); // placeholder for the length
                ms.Write(BitConverter.GetBytes(bitmapCount), 0, 4); // write the number of bitmaps in the list
                ms.Write(ascii.GetBytes("G264"), 0, 4); // 

                int fshlen = 16 + (8 * bitmapCount); // fsh length

                BitmapEntry entry = null;                
                FshImageFormat format;
                int bmpw, bmph, width, height, mipCount;
                for (int i = 0; i < bitmapCount; i++)
                {
                    //write directory
                    entry = this.bitmaps[i];
                    ms.Write(ascii.GetBytes(entry.DirName), 0, 4); // directory id
                    ms.Write(BitConverter.GetBytes(fshlen), 0, 4); // Write the Entry offset 

                    fshlen += 16; // skip the entry header length
                    
                    mipCount = entry.EmbeddMipmapCount;
                    format = entry.BmpType;
                    width = entry.Bitmap.Width;
                    height = entry.Bitmap.Height;

                    for (int j = 0; j <= mipCount; j++)
                    {
                        bmpw = (width >> j);
                        bmph = (height >> j);

                        if (format == FshImageFormat.DXT1)
                        {
                            bmpw += (4 - bmpw) & 3; // 4x4 blocks
                            bmph += (4 - bmph) & 3;
                        }

                        int len = GetBmpDataSize(bmpw, bmph, format);

                        if (!entry.packedMbp && format != FshImageFormat.DXT1)
                        {
                            len += (len & 15);
                        }

                        fshlen += len;
                    }
                }
                
                Bitmap bmp, alpha;
                for (int i = 0; i < bitmapCount; i++)
                {
                    entry = this.bitmaps[i];
                    bmp = entry.Bitmap;
                    alpha = entry.Alpha;
                    format = entry.BmpType;

                    width = bmp.Width;
                    height = bmp.Height;
                    
                    // write entry header
                    ms.Write(BitConverter.GetBytes((int)format), 0, 4); // write the Entry bitmap code
                    ms.Write(BitConverter.GetBytes((ushort)width), 0, 2); // write width
                    ms.Write(BitConverter.GetBytes((ushort)height), 0, 2); //write height

                    mipCount = entry.EmbeddMipmapCount;
                    ushort[] misc = entry.miscHeader;
                    if (misc == null)
                    {
                        misc = new ushort[4] { 0, 0, 0, (ushort)(mipCount << 12) };
                    }

                    for (int m = 0; m < 4; m++)
                    {
                        ms.Write(BitConverter.GetBytes(misc[m]), 0, 2);// write misc data
                    }

                    int realWidth, realHeight;
                    Bitmap srcImage = BlendDXTBitmap(bmp, alpha);
                    Bitmap temp = (Bitmap)srcImage.Clone(); 

                    for (int j = 0; j <= mipCount; j++)
                    {
                        bmpw = realWidth = (width >> j);
                        bmph = realHeight = (height >> j);

                        if (format == FshImageFormat.DXT1) // Maxis files use this
                        {
                            bmpw += (4 - bmpw) & 3; // 4x4 blocks 
                            bmph += (4 - bmph) & 3;
                        }

                        if (j > 0)
                        {
                            if (temp != null)
                            {
                                temp.Dispose();
                                temp = null;
                            }


                            if (format == FshImageFormat.DXT1 && (realWidth < 4 || realHeight < 4))
                            {                            
                                temp = new Bitmap(bmpw, bmph, PixelFormat.Format32bppArgb);
                                using (Bitmap temp2 = SuperSample.GetBitmapThumbnail(srcImage, realWidth, realHeight))
                                using (Graphics g = Graphics.FromImage(temp))
                                {
                                    g.DrawImageUnscaled(temp2, 0, 0);
                                }
                            }
                            else
                            {
                                temp = SuperSample.GetBitmapThumbnail(srcImage, bmpw, bmph);
                            }
                        }

                        BitmapData bd = temp.LockBits(new Rectangle(0, 0, bmpw, bmph), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                        byte* scan0 = (byte*)bd.Scan0.ToPointer();
                        int stride = bd.Stride;

                        int dataLength = GetBmpDataSize(bmpw, bmph, format);
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
					                data = Squish.CompressImage(scan0, stride, bmpw, bmph, flags);
                                }
                                else
                                {
                                    data = DXTComp.CompressFSHToolDXT1(scan0, bmpw, bmph);
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
					                data = Squish.CompressImage(scan0, stride, bmpw, bmph, flags);
                                }
                                else
                                {
                                   data = DXTComp.CompressFSHToolDXT3(scan0, bmpw, bmph);
                                }
                            }
                        }
                        finally
                        {
                            temp.UnlockBits(bd);
                        }

                        if (!entry.packedMbp && format != FshImageFormat.DXT1 ||
                               entry.packedMbp && j == mipCount)
                        {
                            while ((dataLength & 15) > 0)
                            {
                                data[dataLength++] = 0; // pad to 16 bytes?
                            }
                        }

                        ms.Write(data, 0, dataLength);
                    }

                }

                ms.Position = 4L;
                ms.Write(BitConverter.GetBytes((int)ms.Length), 0, 4); // write the files length
                this.rawData = ms.ToArray();
                if (isCompressed)
                {

                    byte[] compbuf = QfsComp.Comp(rawData);
                    if ((compbuf != null) && (compbuf.LongLength < ms.Length))
                    {
                        output.Write(compbuf, 0, compbuf.Length);
                    }
                    else
                    {
                        isCompressed = false;
                        output.Write(rawData, 0, rawData.Length);
                    }
                }
                else
                {
                    ms.WriteTo(output); // write the memory stream to the file
                }

                
            }

		}

		/// <summary>
		/// Test if the fsh only contains DXT1 or DXT3 items
		/// </summary>
		/// <returns>True if successful otherwise false</returns>
		public bool IsDXTFsh()
		{
			foreach (BitmapEntry item in this.bitmaps)
			{
                if (item.BmpType != FshImageFormat.DXT3 && item.BmpType != FshImageFormat.DXT1)
				{
					return false;
				}
			}
			return true;
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
				throw new ArgumentNullException("imageBytes");

			if (imageBytes.Length <= 4)
				throw new FormatException(Resources.InvalidFshFile);

			MemoryStream ms = null;

			try
			{
				byte[] rawData = null;
				if (((imageBytes[0] & 0xfe) == 16 && imageBytes[1] == 0xfb) ||
					((imageBytes[4] & 0xfe) == 16 && imageBytes[5] == 0xfb)) 
				{
					rawData = QfsComp.Decomp(imageBytes);
				}
				else
				{
					rawData = imageBytes;
				}
								   
				ms = new MemoryStream(rawData);

				using (BinaryReader br = new BinaryReader(ms))
				{				
                    ms = null;

					byte[] SHPI = br.ReadBytes(4);
					if (Encoding.ASCII.GetString(SHPI) != "SHPI")
					{
						throw new FormatException(Resources.InvalidFshHeader);
					}
					FSHHeader header = new FSHHeader();
					header.SHPI = SHPI;
					header.size = br.ReadInt32();
					header.imageCount = br.ReadInt32();
					header.dirID = br.ReadBytes(4);

					int fshSize = header.size;
					int nBmp = header.imageCount;
					FSHDirEntry[] dirs = new FSHDirEntry[nBmp];

					for (int i = 0; i < nBmp; i++)
					{
						dirs[i].name = br.ReadBytes(4);
						dirs[i].offset = br.ReadInt32();
					}

					for (int i = 0; i < nBmp; i++)
					{
						FSHDirEntry dir = dirs[i];
						br.BaseStream.Seek((long)dir.offset, SeekOrigin.Begin);
						EntryHeader eHeader = new EntryHeader(br);

						int code = (eHeader.Code & 0x7f);

						if ((code == 0x7b) ||  (code == 0x7e) || (code == 0x78) || (code == 0x6d))
						{
							throw new FormatException(Resources.UnsupportedFshType); // bail on the non SC4 formats
						}

						bool isBmp = ((code == 0x7d) || (code == 0x7f) || (code == 0x60) || (code == 0x61));

						if (isBmp && eHeader.Width < 128 && eHeader.Height < 128)
						{
							return false;
						}
					}
				}

			}
			finally
			{
				if (ms != null)
				{
					ms.Dispose();
					ms = null;
				}
			}

			return true;
		}

		private bool disposed;
		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
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
