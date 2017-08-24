using System;
using System.IO;

namespace FshDatIO
{
    /// <summary>
    /// Encapsulates a DBPF compression directory entry
    /// </summary>
    internal sealed class DirectoryEntry
    {
        private readonly uint type;
        private readonly uint group;
        private readonly uint instance;
        private readonly uint unCompressedSize;

        public const int SizeOf = 16;

        /// <summary>
        /// Gets the type id of the compression directory.
        /// </summary>
        public uint Type
        {
            get
            {
                return this.type;
            }
        }

        /// <summary>
        /// Gets the group id of the compression directory.
        /// </summary>
        public uint Group
        {
            get
            {
                return this.group;
            }
        }

        /// <summary>
        /// Gets the instance id of the compression directory.
        /// </summary>
        public uint Instance
        {
            get
            {
                return this.instance;
            }
        }

        /// <summary>
        /// Gets the uncompressed size of the file.
        /// </summary>
        public uint UncompressedSize
        {
            get
            {
                return this.unCompressedSize;
            }
        }

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

        /// <summary>
        /// Saves the <see cref="DirectoryEntry"/> to the specified stream.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> where the entry will be saved..</param>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> is null.</exception>
        public void Save(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            stream.WriteUInt32(this.type);
            stream.WriteUInt32(this.group);
            stream.WriteUInt32(this.instance);
            stream.WriteUInt32(this.unCompressedSize);
        }
    }

}
