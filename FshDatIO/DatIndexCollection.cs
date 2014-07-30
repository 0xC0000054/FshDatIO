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

        public int Find(uint type, uint group, uint instance)
        {
            IList<DatIndex> indices = base.Items;

            for (int i = 0; i < indices.Count; i++)
            {
                DatIndex index = indices[i];
                if (index.Type == type && index.Group == group && index.Instance == instance)
                {
                    return i;
                }
            }

            return -1;
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
