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
                bitmap = value;
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
                alpha = value;
            }
        }

        /// <summary>
        /// Gets or sets the FSHBitmapType of the entry.
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
        /// Gets or sets the name of the <see cref="FSHDirEntry"/> dir.
        /// </summary>
        /// <value>
        /// The name of the dir.
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
        /// Gets or sets the embedded mipmap count.
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
            this.disposed = false;
        }

        private bool disposed;
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                if (bitmap != null)
                {
                    bitmap.Dispose();
                    bitmap = null;
                }

                if (alpha != null)
                {
                    alpha.Dispose();
                    alpha = null;
                }

                this.disposed = true;
            }
            GC.SuppressFinalize(this);

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
