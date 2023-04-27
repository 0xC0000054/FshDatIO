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
