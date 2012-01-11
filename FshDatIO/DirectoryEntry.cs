using System.IO;

namespace FshDatIO
{
    /// <summary>
    /// Encapsulates a DBPF compression directory entry
    /// </summary>
    internal sealed class DirectoryEntry
    {
        private uint type;
        private uint group;
        private uint instance;
        private uint unCompressedSize;

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectoryEntry"/> class.
        /// </summary>
        /// <param name="type">The type id of the entry.</param>
        /// <param name="group">The group id of the entry.</param>
        /// <param name="instance">The instance id of the entry.</param>
        /// <param name="unCompressedSize">The uncompressed size of the entry.</param>
        public DirectoryEntry(uint type, uint group, uint instance, uint unCompressedSize)
        {
            this.type = type;
            this.group = group;
            this.instance = instance;
            this.unCompressedSize = unCompressedSize;
        }

        public void Save(BinaryWriter bw)
        {
            bw.Write(this.type);
            bw.Write(this.group);
            bw.Write(this.instance);
            bw.Write(this.unCompressedSize);
        }
    }

}
