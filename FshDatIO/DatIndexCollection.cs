using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FshDatIO
{
    internal sealed class DatIndexCollection : Collection<DatIndex>
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
            IList<DatIndex> indices = base.Items;

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
            if (startIndex >= 0 && startIndex < base.Items.Count)
            {
                IList<DatIndex> indices = base.Items;

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
            ((List<DatIndex>)base.Items).RemoveAll(predicate);
        }

        public ReadOnlyCollection<DatIndex> AsReadOnly()
        {
            return new ReadOnlyCollection<DatIndex>(base.Items);
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
                            base.Items[i].FileItem.Dispose();
                            base.Items[i].FileItem = null;
                        }
                    }
                }
            }
        }

    }
}
