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
        private uint majorVersion;
        private uint minorVersion;
        private uint userMajorVersion;
        private uint userMinorVersion;
        private uint flags;
        private uint dateCreated;
        private uint dateModified;
        private uint indexMajorVersion;
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
        private const uint SC4FormatMajorVersion = 1;
        /// <summary>
        /// The minor version of the Sim City 4 DBPF format.
        /// </summary>
        private const uint SC4FormatMinorVersion = 0;
        /// <summary>
        /// The index major version of the Sim City 4 DBPF format.
        /// </summary>
        private const uint SC4IndexMajorVersion = 7;

        /// <summary>
        /// Gets the header major version.
        /// </summary>
        public uint MajorVersion
        {
            get
            {
                return this.majorVersion;
            }
        }

        /// <summary>
        /// Gets the header minor version.
        /// </summary>
        public uint MinorVersion
        {
            get
            {
                return this.minorVersion;
            }
        }

        /// <summary>
        /// Gets the user major version.
        /// </summary>
        public uint UserMajorVersion
        {
            get
            {
                return this.userMajorVersion;
            }
        }

        /// <summary>
        /// Gets the user minor version.
        /// </summary>
        public uint UserMinorVersion
        {
            get
            {
                return this.userMinorVersion;
            }
        }

        /// <summary>
        /// Gets the header flags.
        /// </summary>
        public uint Flags
        {
            get
            {
                return this.flags;
            }
        }

        /// <summary>
        /// The Date the DatFile was Created (In Unix Timestamp format, the number of seconds since 1 / 1 / 1970) 
        /// </summary>
        public uint DateCreated
        {
            get
            {
                return this.dateCreated;
            }
            internal set
            {
                this.dateCreated = value;
            }
        }

        /// <summary>
        /// The Date the DatFile was Modified (In Unix Timestamp format, the number of seconds since 1 / 1 / 1970) 
        /// </summary>
        public uint DateModified
        {
            get
            {
                return this.dateModified;
            }
            internal set
            {
                this.dateModified = value;
            }
        }

        /// <summary>
        /// Gets the major version of the index table.
        /// </summary>
        public uint IndexMajorVersion
        {
            get
            {
                return this.indexMajorVersion;
            }
        }

        /// <summary>
        /// Gets the number of entries in the file.
        /// </summary>
        public uint Entries
        {
            get
            {
                return this.entries;
            }
            internal set
            {
                this.entries = value;
            }
        }

        /// <summary>
        /// Gets the location of the index table.
        /// </summary>
        public uint IndexLocation
        {
            get
            {
                return this.indexLocation;
            }
            internal set
            {
                this.indexLocation = value;
            }
        }

        /// <summary>
        /// Gets the size of the index table.
        /// </summary>
        public uint IndexSize
        {
            get
            {
                return this.indexSize;
            }
            internal set
            {
                this.indexSize = value;
            }
        }

        /// <summary>
        /// Gets the hole count.
        /// </summary>
        public uint HoleCount
        {
            get
            {
                return this.holeCount;
            }
        }

        /// <summary>
        /// Gets the location of the hole index table.
        /// </summary>
        public uint HoleIndexLocation
        {
            get
            {
                return this.holeIndexLocation;
            }
        }

        /// <summary>
        /// Gets the size of the hole index table.
        /// </summary>
        public uint HoleIndexSize
        {
            get
            {
                return this.holeIndexSize;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DatHeader"/> class.
        /// </summary>
        internal DatHeader()
        {
            this.majorVersion = SC4FormatMajorVersion;
            this.minorVersion = SC4FormatMinorVersion;
            this.userMajorVersion = 0;
            this.userMinorVersion = 0;
            this.flags = 0;
            this.dateCreated = 0;
            this.dateModified = 0;
            this.indexMajorVersion = SC4IndexMajorVersion;
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

            this.majorVersion = input.ReadUInt32();
            this.minorVersion = input.ReadUInt32();
            if (this.majorVersion != SC4FormatMajorVersion || this.minorVersion != SC4FormatMinorVersion)
            {
                throw new DatFileException(string.Format(CultureInfo.CurrentCulture, Resources.UnsupportedDBPFVersion, this.majorVersion, this.minorVersion));
            }

            this.userMajorVersion = input.ReadUInt32();
            this.userMinorVersion = input.ReadUInt32();
            this.flags = input.ReadUInt32();
            this.dateCreated = input.ReadUInt32();
            this.dateModified = input.ReadUInt32();
            this.indexMajorVersion = input.ReadUInt32();
            if (this.indexMajorVersion != SC4IndexMajorVersion)
            {
                throw new DatFileException(string.Format(CultureInfo.CurrentCulture, Resources.UnsupportedIndexVersion, this.indexMajorVersion));
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
            stream.WriteUInt32(this.majorVersion);
            stream.WriteUInt32(this.minorVersion);
            stream.WriteUInt32(this.userMajorVersion);
            stream.WriteUInt32(this.userMinorVersion);
            stream.WriteUInt32(this.flags);
            stream.WriteUInt32(this.dateCreated);
            stream.WriteUInt32(this.dateModified);
            stream.WriteUInt32(this.indexMajorVersion);
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
