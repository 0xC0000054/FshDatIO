using System;
using System.Collections.ObjectModel;
using System.Drawing;

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
                return bitmap;
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
                return alpha;
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
                return bmpType;
            }
            set
            {
                bmpType = value;
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
                return dirName;
            }
            set
            {
                dirName = value;
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
                return embeddedMipmapCount;
            }
            internal set
            {
                embeddedMipmapCount = value;
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
                return attachments;
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
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!disposed && disposing)
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

            while (width > 1 && height > 1)
            {
                mips++;
                width /= 2;
                height /= 2;
            }

            if (mips > 15)
            {
                mips = 0; // FSH images can have a maximum of 15 mipmaps.
            }

            if (bmpType == FshImageFormat.DXT1)
            {
                this.packedMbp = true;
            }

            this.embeddedMipmapCount = mips;
        }

    }
}
