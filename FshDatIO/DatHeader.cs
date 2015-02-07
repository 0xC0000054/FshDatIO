using FshDatIO.Properties;
using System;
using System.Globalization;
using System.IO;

namespace FshDatIO
{
    /// <summary>
    /// Encapsulates the header of a DBPF file.
    /// </summary>
    public sealed class DatHeader
    {
        private uint vMajor;
        private uint vMinor;
        private uint uVMajor;
        private uint uVMinor;
        private uint flags;
        private uint dateCreated;
        private uint dateModified;
        private uint indexVMajor; // index major version always 7
        private uint entries;
        private uint indexLocation;
        private uint indexSize;
        private uint holeCount;
        private uint holeIndexLocation;
        private uint holeIndexSize;

        /// <summary>
        /// The DBPF signature in little endian format.
        /// </summary>
        private const uint DBPFSignature = 0x46504244U;
        /// <summary>
        /// The major version of the Sim City 4 DBPF format.
        /// </summary>
        private const uint FileMajorVersion = 1;
        /// <summary>
        /// The minor version of the Sim City 4 DBPF format.
        /// </summary>
        private const uint FileMinorVersion = 0;
        /// <summary>
        /// The index major version of the Sim City 4 DBPF format.
        /// </summary>
        private const uint IndexMajorVersion = 7;

        /// <summary>
        /// Gets the header major version.
        /// </summary>
        public uint VersionMajor
        {
            get
            {
                return vMajor;
            }
        }

        /// <summary>
        /// Gets the header minor version.
        /// </summary>
        public uint VersionMinor
        {
            get
            {
                return vMinor;
            }
        }

        /// <summary>
        /// Gets the major user version.
        /// </summary>
        public uint UserVersionMajor
        {
            get
            {
                return uVMajor;
            }
        }

        /// <summary>
        /// Gets the minor user version.
        /// </summary>
        public uint UserVersionMinor
        {
            get
            {
                return uVMinor;
            }
        }

        /// <summary>
        /// Gets the header flags.
        /// </summary>
        public uint Flags
        {
            get
            {
                return flags;
            }
        }

        /// <summary>
        /// The Date the DatFile was Created (In Unix Timestamp format, the number of seconds since 1 / 1 / 1970) 
        /// </summary>
        public uint DateCreated
        {
            get
            {
                return dateCreated;
            }
            internal set
            {
                dateCreated = value;
            }
        }

        /// <summary>
        /// The Date the DatFile was Modified (In Unix Timestamp format, the number of seconds since 1 / 1 / 1970) 
        /// </summary>
        public uint DateModified
        {
            get
            {
                return dateModified;
            }
            internal set
            {
                dateModified = value;
            }
        }

        /// <summary>
        /// Gets the  major version of the index table.
        /// </summary>
        public uint IndexVersionMajor
        {
            get
            {
                return indexVMajor;
            }
        }

        /// <summary>
        /// Gets the number of entries in the file.
        /// </summary>
        public uint Entries
        {
            get
            {
                return entries;
            }
            internal set
            {
                entries = value;
            }
        }

        /// <summary>
        /// Gets the location of the index table.
        /// </summary>
        public uint IndexLocation
        {
            get
            {
                return indexLocation;
            }
            internal set
            {
                indexLocation = value;
            }
        }

        /// <summary>
        /// Gets the size of the index table.
        /// </summary>
        public uint IndexSize
        {
            get
            {
                return indexSize;
            }
            internal set
            {
                indexSize = value;
            }
        }

        /// <summary>
        /// Gets the hole count.
        /// </summary>
        public uint HoleCount
        {
            get
            {
                return holeCount;
            }
        }

        /// <summary>
        /// Gets the location of the hole index table.
        /// </summary>
        public uint HoleIndexLocation
        {
            get
            {
                return holeIndexLocation;
            }
        }

        /// <summary>
        /// Gets the size of the hole index table.
        /// </summary>
        public uint HoleIndexSize
        {
            get
            {
                return holeIndexSize;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DatHeader"/> class.
        /// </summary>
        internal DatHeader()
        {
            this.vMajor = FileMajorVersion;
            this.vMajor = FileMinorVersion;
            this.uVMajor = 0;
            this.uVMinor = 0;
            this.indexVMajor = IndexMajorVersion;
            this.entries = 0;
            this.indexLocation = 96;
            this.indexSize = 0;
            this.holeCount = 0;
            this.holeIndexSize = 0;
            this.holeIndexLocation = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DatHeader" /> class.
        /// </summary>
        /// <param name="input">The Stream to read from.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when the BinaryReader is null.</exception>
        /// <exception cref="DatHeaderException">The header signature is invalid.</exception>
        /// <exception cref="DatFileException">The DBPF format version is not supported.</exception>
        internal DatHeader(Stream input)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }

            input.Position = 0L;
            
            uint sig = input.ReadUInt32();
            if (sig != DBPFSignature)
            {
                throw new DatHeaderException(Resources.DatHeaderInvalidIdentifer);
            }

            this.vMajor = input.ReadUInt32();
            if (this.vMajor != FileMajorVersion)
            {
                throw new DatFileException(string.Format(CultureInfo.CurrentCulture, Resources.UnsupportedDBPFVersion, this.vMajor, "0"));
            }

            this.vMinor = input.ReadUInt32();
            if (this.vMinor != FileMinorVersion)
            {
                throw new DatFileException(string.Format(CultureInfo.CurrentCulture, Resources.UnsupportedDBPFVersion, this.vMajor, this.vMinor));
            }

            this.uVMajor = input.ReadUInt32();
            this.uVMinor = input.ReadUInt32();
            this.flags = input.ReadUInt32();
            this.dateCreated = input.ReadUInt32();
            this.dateModified = input.ReadUInt32();
            this.indexVMajor = input.ReadUInt32();
            if (this.indexVMajor != IndexMajorVersion)
            {
                throw new DatFileException(string.Format(CultureInfo.CurrentCulture, Resources.UnsupportedIndexVersion, this.vMajor, this.vMinor));
            }

            this.entries = input.ReadUInt32();
            this.indexLocation = input.ReadUInt32();
            this.indexSize = input.ReadUInt32();
            this.holeCount = input.ReadUInt32();
            this.holeIndexLocation = input.ReadUInt32();
            this.holeIndexSize = input.ReadUInt32();
        }

        /// <summary>
        /// Saves the DatHeader.
        /// </summary>
        /// <param name="stream">The Stream to save to</param>
        /// <exception cref="System.ArgumentNullException">Thrown when the BinaryWriter is null.</exception>
        internal void Save(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }
            
            stream.WriteUInt32(DBPFSignature);
            stream.WriteUInt32(this.vMajor);
            stream.WriteUInt32(this.vMinor);
            stream.WriteUInt32(this.uVMajor);
            stream.WriteUInt32(this.uVMinor);
            stream.WriteUInt32(this.flags);
            stream.WriteUInt32(this.dateCreated);
            stream.WriteUInt32(this.dateModified);
            stream.WriteUInt32(this.indexVMajor);
            stream.WriteUInt32(this.entries);
            stream.WriteUInt32(this.indexLocation);
            stream.WriteUInt32(this.indexSize);
            stream.WriteUInt32(this.holeCount);
            stream.WriteUInt32(this.holeIndexLocation);
            stream.WriteUInt32(this.holeIndexSize);
            byte[] reservedBytes = new byte[36];
            stream.Write(reservedBytes, 0, 36); // reserved byte padding
        }
    }

}
