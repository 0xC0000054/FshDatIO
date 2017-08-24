using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FshDatIO
{
    internal sealed class DirectoryEntryCollection : Collection<DirectoryEntry>
    {
        public DirectoryEntryCollection() : this(0)
        {
        }

        public DirectoryEntryCollection(int count) : base(new List<DirectoryEntry>(count))
        {
        }

        public DirectoryEntry Find(uint type, uint group, uint instance)
        {
            IList<DirectoryEntry> items = Items;

            for (int i = 0; i < items.Count; i++)
            {
                DirectoryEntry entry = items[i];
                if (entry.Type == type && entry.Group == group && entry.Instance == instance)
                {
                    return entry;
                }
            }

            return null;
        }
    }
}
