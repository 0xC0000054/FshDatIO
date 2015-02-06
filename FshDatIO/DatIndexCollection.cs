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
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                this.disposed = true;

                if (disposing)
                {
                    for (int i = 0; i < Count; i++)
                    {
                        if (Items[i].FileItem != null)
                        {
                            Items[i].FileItem.Dispose();
                            Items[i].FileItem = null;
                        }
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
                if (x != null)
                {
                    if (y != null)
                    { 
                        if (x.Location < y.Location)
                        {
                            return -1;
                        }
                        else if (x.Location > y.Location)
                        {
                            return 1;
                        }
                        else
                        {
                            // The file locations should never be equal.
                            return 0;
                        }
                    }

                    return 1;
                }
                if (y != null)
                {
                    return -1;
                }

                return 0;
            }
        }

    }
}
