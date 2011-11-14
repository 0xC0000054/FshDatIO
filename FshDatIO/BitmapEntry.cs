using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using FSHLib;

namespace FshDatIO
{
    public sealed class BitmapEntry : IDisposable
    {
        private Bitmap bitmap;
        private Bitmap alpha;
        private FSHBmpType bmpType;
        private string dirName;

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

        public FSHBmpType BmpType
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

        private BitmapEntry(BitmapEntry cloneMe)
        {
            this.bitmap = cloneMe.bitmap.Clone() as Bitmap;
            this.alpha = cloneMe.alpha.Clone() as Bitmap;
            this.bmpType = cloneMe.bmpType;
            this.dirName = cloneMe.dirName;
        }

        public BitmapEntry Clone()
        { 
            return new BitmapEntry(this);
        }

        public BitmapEntry()
        {
            this.bitmap = null;
            this.alpha = null;
            this.bmpType = FSHBmpType.DXT1;
            this.disposed = false;
        }

        private bool disposed;
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

        public BitmapItem ToBitmapItem()
        {
            BitmapItem item = new BitmapItem();
            item.Bitmap = this.bitmap.Clone() as Bitmap;
            item.Alpha = this.alpha.Clone() as Bitmap;
            item.BmpType = this.bmpType;

            if (!string.IsNullOrEmpty(this.dirName))
            {
                item.SetDirName(this.dirName);
            }
            else
            {
                item.SetDirName("FiSH");
            }

            return item;
        }

        /// <summary>
        /// Creates a new BitmapEntry from the specified BitmapItem.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>The new BitmapEntry or null.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown when the item is null.</exception>
        public static BitmapEntry FromBitmapItem(BitmapItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            BitmapEntry entry = null;
            using (BitmapEntry temp = new BitmapEntry())
            {
                Rectangle cloneRect = new Rectangle(0, 0, item.Bitmap.Width, item.Bitmap.Height);
                temp.bitmap = item.Bitmap.Clone(cloneRect, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                temp.alpha = item.Alpha.Clone(cloneRect, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                temp.bmpType = item.BmpType;
                temp.dirName = Encoding.ASCII.GetString(item.DirName);

                entry = temp.Clone();
            }

            return entry; 
        }
    }
}
