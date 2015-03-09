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

        /// <summary>
        /// Removes all elements from the <see cref="T:System.Collections.ObjectModel.Collection`1" />.
        /// </summary>
        protected override void ClearItems()
        {
            IList<BitmapEntry> items = Items;
            for (int i = 0; i < items.Count; i++)
            {
                BitmapEntry item = items[i];
                if (item != null)
                {
                    item.Dispose();
                    item = null;
                }
            }

            base.ClearItems();
        }

        /// <summary>
        /// Removes the element at the specified index of the <see cref="T:System.Collections.ObjectModel.Collection`1" />.
        /// </summary>
        /// <param name="index">The zero-based index of the element to remove.</param>
        protected override void RemoveItem(int index)
        {
            BitmapEntry entry = Items[index];

            if (entry != null)
            {
                entry.Dispose();
            }
            
            base.RemoveItem(index);
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
            if (!this.disposed)
            {
                this.disposed = true;

                if (disposing)
                {
                    IList<BitmapEntry> items = Items;
                    for (int i = 0; i < items.Count; i++)
                    {
                        BitmapEntry item = items[i];
                        if (item != null)
                        {
                            item.Dispose();
                            item = null;
                        }
                    }
                }
            }
        }

    }
}
