using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace FshDatIO
{
    internal sealed class DirectoryEntryCollection 
    {
        private DirectoryEntry[] dirs;
        /// <summary>
        /// Initializes a new instance of the <see cref="DirectoryEntryCollection"/> class.
        /// </summary>
        /// <param name="capacity">The capacity of the item array.</param>
        public DirectoryEntryCollection(int capacity)
        {
            dirs = new DirectoryEntry[capacity];
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="DirectoryEntryCollection"/> class.
        /// </summary>
        /// <param name="entries">The entries to copy from.</param>
        public DirectoryEntryCollection(IList<DirectoryEntry> entries)
        {
            this.dirs = new DirectoryEntry[entries.Count];
            this.dirs.CopyTo(dirs, 0);
        }

        /// <summary>
        /// Inserts the DirectoryEntry at specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="entry">The entry.</param>
        public void Insert(int index, DirectoryEntry entry)
        {
            if (index < 0 || index >= dirs.Length)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            this.dirs[index] = entry;
        }

        /// <summary>
        /// Determines whether the collection contains the specified TGI.
        /// </summary>
        /// <param name="type">The type id.</param>
        /// <param name="group">The group id.</param>
        /// <param name="instance">The instance id.</param>
        /// <returns>
        ///   <c>true</c> if the specified TGI is found; otherwise, <c>false</c>.
        /// </returns>
        public bool Contains(uint type, uint group, uint instance)
        {
            for (int i = 0; i < dirs.Length; i++)
            {
                if (dirs[i].Equals(type, group, instance))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the number of items on the collection.
        /// </summary>
        public int Count
        {
            get 
            {
                return dirs.Length;
            }
        }
    }
}
