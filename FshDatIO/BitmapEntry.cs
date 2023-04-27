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
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;

namespace FshDatIO
{
    /// <summary>
    /// Encapsulates the images within a FSHImageWrapper
    /// </summary>
    public sealed class BitmapEntry : IDisposable
    {
        private Bitmap bitmap;
        private Bitmap alpha;
        private FshImageFormat bmpType;
        private string dirName;
        private int embeddedMipmapCount;
        private ReadOnlyCollection<FshAttachment> attachments;
        internal bool packedMbp;
        internal ushort[] miscHeader;

        private bool disposed;

        /// <summary>
        /// Gets or sets the bitmap.
        /// </summary>
        /// <value>
        /// The bitmap.
        /// </value>
        public Bitmap Bitmap
        {
            get
            {
                return this.bitmap;
            }
            set
            {
                if (this.bitmap != null)
                {
                    this.bitmap.Dispose();
                    this.bitmap = null;
                }

                this.bitmap = value;
            }
        }

        /// <summary>
        /// Gets or sets the alpha bitmap.
        /// </summary>
        /// <value>
        /// The alpha.
        /// </value>
        public Bitmap Alpha
        {
            get
            {
                return this.alpha;
            }
            set
            {
                if (this.alpha != null)
                {
                    this.alpha.Dispose();
                    this.alpha = null;
                }

                this.alpha = value;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="FshImageFormat"/> of the entry.
        /// </summary>
        /// <value>
        /// The FSHBitmapType.
        /// </value>
        public FshImageFormat BmpType
        {
            get
            {
                return this.bmpType;
            }
            set
            {
                this.bmpType = value;
            }
        }

        /// <summary>
        /// Gets or sets the name of the <see cref="FSHDirEntry"/> directory.
        /// </summary>
        /// <value>
        /// The name of the directory.
        /// </value>
        public string DirName
        {
            get
            {
                return this.dirName;
            }
            set
            {
                this.dirName = value;
            }
        }

        /// <summary>
        /// Gets the embedded mipmap count.
        /// </summary>
        /// <value>
        /// The embedded mipmap count.
        /// </value>
        public int EmbeddedMipmapCount
        {
            get
            {
                return this.embeddedMipmapCount;
            }
        }

        /// <summary>
        /// Gets the attachments.
        /// </summary>
        /// <value>
        /// The attachments.
        /// </value>
        public ReadOnlyCollection<FshAttachment> Attachments
        {
            get
            {
                return this.attachments;
            }
            internal set
            {
                this.attachments = value;
            }
        }

        private BitmapEntry(BitmapEntry cloneMe)
        {
            this.bitmap = cloneMe.bitmap.Clone() as Bitmap;
            this.alpha = cloneMe.alpha.Clone() as Bitmap;
            this.bmpType = cloneMe.bmpType;
            this.dirName = cloneMe.dirName;
            this.embeddedMipmapCount = cloneMe.embeddedMipmapCount;
            this.miscHeader = cloneMe.miscHeader;
            this.packedMbp = cloneMe.packedMbp;
            this.attachments = cloneMe.attachments;
        }

        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns></returns>
        public BitmapEntry Clone()
        {
            return new BitmapEntry(this);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BitmapEntry"/> class.
        /// </summary>
        public BitmapEntry()
        {
            this.bitmap = null;
            this.alpha = null;
            this.bmpType = FshImageFormat.DXT1;
            this.dirName = string.Empty;
            this.embeddedMipmapCount = 0;
            this.packedMbp = false;
            this.miscHeader = null;
            this.attachments = null;
            this.disposed = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BitmapEntry" /> class.
        /// </summary>
        /// <param name="bitmap">The bitmap.</param>
        /// <param name="alpha">The alpha.</param>
        /// <param name="bmpType">The <see cref="FshImageFormat" /> of the entry.</param>
        /// <param name="dirName">The directory name of the entry.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="bitmap"/> is null.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="alpha"/> is null.</exception>
        public BitmapEntry(Bitmap bitmap, Bitmap alpha, FshImageFormat bmpType, string dirName)
        {
            if (bitmap == null)
            {
                throw new ArgumentNullException("bitmap");
            }

            if (alpha == null)
            {
                throw new ArgumentNullException("alpha");
            }

            Rectangle imageRect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            this.bitmap = bitmap.Clone(imageRect, PixelFormat.Format24bppRgb);
            this.alpha = alpha.Clone(imageRect, PixelFormat.Format24bppRgb);
            this.bmpType = bmpType;
            this.dirName = dirName;
            this.embeddedMipmapCount = 0;
            this.packedMbp = false;
            this.miscHeader = null;
            this.attachments = null;
            this.disposed = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BitmapEntry"/> class.
        /// </summary>
        /// <param name="format">The format of the entry.</param>
        /// <param name="dirName">The name of the parent <see cref="FSHDirEntry"/>.</param>
        /// <param name="embeddedMipCount">The embedded mipmap count.</param>
        /// <param name="mipsPacked"><c>true</c> if the embedded mapmaps are packed; otherwise, <c>false</c>.</param>
        /// <param name="miscData">The misc data from the <see cref="EntryHeader"/>.</param>
        internal BitmapEntry(FshImageFormat format, string dirName, int embeddedMipCount, bool mipsPacked, ushort[] miscData)
        {
            this.bitmap = null;
            this.alpha = null;
            this.bmpType = format;
            this.dirName = dirName;
            this.embeddedMipmapCount = embeddedMipCount;
            this.packedMbp = mipsPacked;
            this.miscHeader = miscData;
            this.attachments = null;
            this.disposed = false;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (!this.disposed)
            {
                this.disposed = true;

                if (this.bitmap != null)
                {
                    this.bitmap.Dispose();
                    this.bitmap = null;
                }

                if (this.alpha != null)
                {
                    this.alpha.Dispose();
                    this.alpha = null;
                }
            }
        }

        /// <summary>
        /// Calculates the mipmap count for the item.
        /// </summary>
        public void CalculateMipmapCount()
        {
            int width = bitmap.Width;
            int height = bitmap.Height;

            int mips = 0;

            // The image must be divisible by 2.
            if ((width & 1) == 0 && (height & 1) == 0)
            {
                while (width > 1 && height > 1)
                {
                    mips++;
                    width /= 2;
                    height /= 2;
                }

                int mipScale = 1 << mips;

                // The image must be divisible by the number of mipmaps, and the total number of mipmaps must be less than 15.
                if (((bitmap.Width % mipScale) != 0 || (bitmap.Height % mipScale) != 0) || mips > 15)
                {
                    mips = 0;
                }                
            }

            this.embeddedMipmapCount = mips;
            if (this.miscHeader != null)
            {
                int existingMips = (this.miscHeader[3] >> 12) & 0x0f;

                if (this.embeddedMipmapCount != existingMips)
                {
                    this.miscHeader[3] = (ushort)(this.embeddedMipmapCount << 12); 
                }
            }
        }

    }
}
