using System;
using System.IO;
using System.Text;

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
        private uint indexLoc;
        private uint indexSize;
        private uint holeCount;
        private uint holeIdxLoc;
        private uint holeSize;

        /// <summary>
        /// The DBPF signature in little endian format.
        /// </summary>
        private const uint DBPFSignature = 0x46504244U;

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
        /// Gets the  major version of the index.
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
        /// Gets the start location of the index.
        /// </summary>
        public uint IndexLocation
        {
            get
            {
                return indexLoc;
            }
            internal set
            {
                indexLoc = value;
            }
        }
        /// <summary>
        /// Gets the size of the index.
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
        /// Gets the hole index location.
        /// </summary>
        public uint HoleIndexLoc
        {
            get
            {
                return holeIdxLoc;
            }
        }
        /// <summary>
        /// Gets the size of the hole.
        /// </summary>
        public uint HoleSize
        {
            get
            {
                return holeSize;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DatHeader"/> class.
        /// </summary>
        internal DatHeader()
        {
            this.vMajor = 1;
            this.vMajor = 0;
            this.uVMajor = 0;
            this.uVMinor = 0;
            this.indexVMajor = 7;
            this.entries = 0;
            this.indexLoc = 96;
            this.indexSize = 0;
            this.holeCount = 0;
            this.holeSize = 0;
            this.holeIdxLoc = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DatHeader"/> class.
        /// </summary>
        /// <param name="reader">The BinaryReader to read from.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when the BinaryReader is null.</exception>
        /// <exception cref="FshDatIO.DatHeaderException">Thrown when the header signature is invalid.</exception>
        internal DatHeader(BinaryReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            reader.BaseStream.Position = 0L;
            this.Load(reader);
        }

        /// <summary>
        /// Loads the DatHeader from specified BinaryReader.
        /// </summary>
        /// <param name="reader">The BinaryReader to read from.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when the BinaryReader is null.</exception>
        /// <exception cref="FshDatIO.DatHeaderException">Thrown when the header signature is invalid.</exception>
        private void Load(BinaryReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("br", "br is null.");
            }
            
            uint sig = reader.ReadUInt32();
            if (sig != DBPFSignature)
            {
                throw new DatHeaderException(FshDatIO.Properties.Resources.DatHeaderInvalidIdentifer);
            }

            this.vMajor = reader.ReadUInt32();
            this.vMinor = reader.ReadUInt32();
            this.uVMajor = reader.ReadUInt32();
            this.uVMinor = reader.ReadUInt32();
            this.flags = reader.ReadUInt32();
            this.dateCreated = reader.ReadUInt32();
            this.dateModified = reader.ReadUInt32();
            this.indexVMajor = reader.ReadUInt32();
            this.entries = reader.ReadUInt32();
            this.indexLoc = reader.ReadUInt32();
            this.indexSize = reader.ReadUInt32();
            this.holeCount = reader.ReadUInt32();
            this.holeIdxLoc = reader.ReadUInt32();
            this.holeSize = reader.ReadUInt32();
        }
        
        /// <summary>
        /// Saves the DatHeader.
        /// </summary>
        /// <param name="writer">The BinaryReader to save to</param>
        /// <exception cref="System.ArgumentNullException">Thrown when the BinaryWriter is null.</exception>
        internal void Save(BinaryWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("bw", "bw is null.");
            }
            
            writer.Write(DBPFSignature);
            writer.Write(this.vMajor);
            writer.Write(this.vMinor);
            writer.Write(this.uVMajor);
            writer.Write(this.uVMinor);
            writer.Write(this.flags);
            writer.Write(this.dateCreated);
            writer.Write(this.dateModified);
            writer.Write(this.indexVMajor);
            writer.Write(this.entries);
            writer.Write(this.indexLoc);
            writer.Write(this.indexSize);
            writer.Write(this.holeCount);
            writer.Write(this.holeIdxLoc);
            writer.Write(this.holeSize);
            byte[] reservedBytes = new byte[36];
            writer.Write(reservedBytes); // reserved byte padding
        }
    }

}
