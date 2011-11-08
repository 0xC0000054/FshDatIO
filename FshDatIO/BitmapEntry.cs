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
        
        public static implicit operator BitmapEntry(BitmapItem item)
        {
            if (item != null && item.Bitmap != null && item.Alpha != null)
            {
                BitmapEntry entry = new BitmapEntry();
                Rectangle cloneRect = new Rectangle(0, 0, item.Bitmap.Width, item.Bitmap.Height);
                entry.bitmap = item.Bitmap.Clone(cloneRect, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                entry.alpha = item.Alpha.Clone(cloneRect, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                entry.bmpType = item.BmpType;
                entry.dirName = Encoding.ASCII.GetString(item.DirName);

                return entry; 
            }

            return null;
        }
    }
}
