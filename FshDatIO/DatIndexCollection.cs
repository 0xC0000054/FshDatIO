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
    internal sealed class DatIndexCollection : Collection<DatIndex>, IDisposable
    {
        private bool disposed;

        public DatIndexCollection() : this(0)
        {
        }

        public DatIndexCollection(int count) : base(new List<DatIndex>(count))
        {
            this.disposed = false;
        }

        protected override void ClearItems()
        {
            IList<DatIndex> indices = Items;

            for (int i = 0; i < indices.Count; i++)
            {
                DatIndex index = indices[i];

                if (index.FileItem != null)
                {
                    index.FileItem.Dispose();
                    index.FileItem = null;
                }
            }
            
            base.ClearItems();
        }

        protected override void RemoveItem(int index)
        {
            DatIndex item = Items[index];

            if (item.FileItem != null)
            {
                item.FileItem.Dispose();
                item.FileItem = null;
            }
            
            base.RemoveItem(index);
        }

        public DatIndex Find(uint type, uint group, uint instance)
        {
            IList<DatIndex> indices = Items;

            for (int i = 0; i < indices.Count; i++)
            {
                DatIndex index = indices[i];
                if (index.Type == type && index.Group == group && index.Instance == instance)
                {
                    return index;
                }
            }

            return null;
        }

        internal int IndexOf(uint type, uint group, uint instance)
        {
            return IndexOf(type, group, instance, 0);
        }

        internal int IndexOf(uint type, uint group, uint instance, int startIndex)
        {
            if (startIndex >= 0 && startIndex < Items.Count)
            {
                IList<DatIndex> indices = Items;

                for (int i = startIndex; i < indices.Count; i++)
                {
                    DatIndex index = indices[i];
                    if (index.Type == type && index.Group == group && index.Instance == instance)
                    {
                        return i;
                    }
                } 
            }

            return -1;
        }

        public void RemoveAll(Predicate<DatIndex> predicate)
        {
            ((List<DatIndex>)Items).RemoveAll(predicate);
        }

        public ReadOnlyCollection<DatIndex> AsReadOnly()
        {
            return new ReadOnlyCollection<DatIndex>(Items);
        }

        /// <summary>
        /// Sorts the collection in ascending order by the file location.
        /// </summary>
        public void SortByLocation()
        {
            ((List<DatIndex>)Items).Sort(new IndexLocationComparer());
        }

        public void Dispose()
        {
            if (!this.disposed)
            {
                this.disposed = true;

                for (int i = 0; i < Count; i++)
                {
                    DatIndex index = Items[i];
                    if (index.FileItem != null)
                    {
                        index.FileItem.Dispose();
                        index.FileItem = null;
                    }
                }
            }
        }

        private sealed class IndexLocationComparer : IComparer<DatIndex>
        {
            public IndexLocationComparer()
            {
            }

            public int Compare(DatIndex x, DatIndex y)
            {
                if (Object.ReferenceEquals(x, y))
                {
                    return 0;
                }
                if (x == null)
                {
                    return -1;
                }
                if (y == null)
                {
                    return 1;
                }

                if (x.Location < y.Location)
                {
                    return -1;
                }
                else if (x.Location > y.Location)
                {
                    return 1;
                }
                
                // The file locations should never be equal.
                return 0;
            }
        }

    }
}
