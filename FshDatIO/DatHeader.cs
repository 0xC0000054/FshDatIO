using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace FshDatIO
{
    public class DatHeader
    {
        uint vMajor;
        uint vMinor;
        uint uVMajor;
        uint uVMinor;
        uint flags;
        uint dateCreated;
        uint dateModified;
        uint indexVMajor; // index major version always 7
        uint entries;
        uint indexLoc;
        uint indexSize;
        uint holeCount;
        uint holeIdxLoc;
        uint holeSize;

        public uint VersionMajor
        {
            get
            {
                return vMajor;
            }
        }
        public uint VersionMinor
        {
            get
            {
                return vMinor;
            }
        }
        public uint UserVersionMajor
        {
            get
            {
                return uVMajor;
            }
        }
        public uint UserVersionMinor
        {
            get
            {
                return uVMinor;
            }
        }
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
        public uint IndexVersionMajor
        {
            get
            {
                return indexVMajor;
            }
        }
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
        public uint IndexLocation
        {
            get
            {
                return indexLoc;
            }
            set
            {
                indexLoc = value;
            }
        }
        public uint IndexSize
        {
            get
            {
                return indexSize;
            }
            set
            {
                indexSize = value;
            }
        }

        public uint HoleCount
        {
            get
            {
                return holeCount;
            }
        }
        public uint HoleIndexLoc
        {
            get
            {
                return holeIdxLoc;
            }
        }
        public uint HoleSize
        {
            get
            {
                return holeSize;
            }
        }

        public DatHeader()
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
        public DatHeader(BinaryReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException("reader", "reader is null.");
            reader.BaseStream.Position = 0L;
            this.Load(reader);
        }

        public void Load(BinaryReader br)
        {
            if (br == null)
                throw new ArgumentNullException("br", "br is null.");
            if (Encoding.ASCII.GetString(br.ReadBytes(4)) != "DBPF")
            {
                throw new DatHeaderException(FshDatIO.Properties.Resources.DatHeaderInvalidIdentifer);
            }
            this.vMajor = br.ReadUInt32();
            this.vMinor = br.ReadUInt32();
            this.uVMajor = br.ReadUInt32();
            this.uVMinor = br.ReadUInt32();
            this.flags = br.ReadUInt32();
            this.dateCreated = br.ReadUInt32();
            this.dateModified = br.ReadUInt32();
            this.indexVMajor = br.ReadUInt32();
            this.entries = br.ReadUInt32();
            this.indexLoc = br.ReadUInt32();
            this.indexSize = br.ReadUInt32();
            this.holeCount = br.ReadUInt32();
            this.holeIdxLoc = br.ReadUInt32();
            this.holeSize = br.ReadUInt32();
        }
        /// <summary>
        /// Saves the DatHeader.
        /// </summary>
        /// <param name="bw">The binaryReader to save to</param>
        public void Save(BinaryWriter bw)
        {
            if (bw == null)
                throw new ArgumentNullException("bw", "bw is null.");
            bw.Write(Encoding.ASCII.GetBytes("DBPF"));
            bw.Write(this.vMajor);
            bw.Write(this.vMinor);
            bw.Write(this.uVMajor);
            bw.Write(this.uVMinor);
            bw.Write(this.flags);
            bw.Write(this.dateCreated);
            bw.Write(this.dateModified);
            bw.Write(this.indexVMajor);
            bw.Write(this.entries);
            bw.Write(this.indexLoc);
            bw.Write(this.indexSize);
            bw.Write(this.holeCount);
            bw.Write(this.holeIdxLoc);
            bw.Write(this.holeSize);
            bw.Write(new byte[36]); // reserved byte padding
        }
    }

}
