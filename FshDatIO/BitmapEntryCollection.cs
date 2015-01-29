using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FshDatIO
{
    /// <summary>
    /// The collection of BitmapEntry items within the FSHImageWrapper
    /// </summary>
    public sealed class BitmapEntryCollection : Collection<BitmapEntry>, IDisposable
    {
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="BitmapEntryCollection"/> class.
        /// </summary>
        internal BitmapEntryCollection() : this(0)
        {
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="BitmapEntryCollection"/> class.
        /// </summary>
        /// <param name="capacity">The capacity.</param>
        internal BitmapEntryCollection(int capacity) : base(new List<BitmapEntry>(capacity))
        {
            this.disposed = false;
        }
        
        protected override void ClearItems()
        {
            IList<BitmapEntry> items = Items;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null)
                {
                    items[i].Dispose();
                    items[i] = null;
                }
            }

            base.ClearItems();
        }

        protected override void RemoveItem(int index)
        {
            BitmapEntry entry = Items[index];

            if (entry != null)
            {
                entry.Dispose();
            }
            
            base.RemoveItem(index);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                this.disposed = true;

                if (disposing)
                {
                    IList<BitmapEntry> items = Items;
                    for (int i = 0; i < items.Count; i++)
                    {
                        if (items[i] != null)
                        {
                            items[i].Dispose();
                            items[i] = null;
                        }
                    }
                }
            }
        }

    }
}
