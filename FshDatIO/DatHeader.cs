using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace FshDatIO
{
    public class DatHeader
    {
        uint vmajor;
        uint vminor;
        uint uvmajor;
        uint uvminor;
        uint flags;
        uint datecreated;
        uint datemodified;
        uint indexvmajor; // index major version always 7
        uint entries;
        uint IndexLoc;
        uint indexsize;
        uint holecount;
        uint HoleIdxLoc;
        uint holesize;

        public uint VersionMajor
        {
            get
            {
                return vmajor;
            }
        }
        public uint VersionMinor
        {
            get
            {
                return vminor;
            }
        }
        public uint UserVersionMajor
        {
            get
            {
                return uvmajor;
            }
        }
        public uint UserVersionMinor
        {
            get
            {
                return uvminor;
            }
        }
        public uint Flags
        {
            get
            {
                return flags;
            }
        }
        public uint DateCreated
        {
            get
            {
                return datecreated;
            }
            internal set
            {
                datecreated = value;
            }
        }
        /// <summary>
        /// The Date the DatFile was Modified (In Unix Timestamp format, the number of seconds since 1 / 1 / 1970) 
        /// </summary>
        public uint DateModified
        {
            get
            {
                return datemodified;
            }
            internal set
            {
                datemodified = value;
            }
        }
        public uint IndexVersionMajor
        {
            get
            {
                return indexvmajor;
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
                return IndexLoc;
            }
            set
            {
                IndexLoc = value;
            }
        }
        public uint IndexSize
        {
            get
            {
                return indexsize;
            }
            set
            {
                indexsize = value;
            }
        }

        public uint HoleCount
        {
            get
            {
                return holecount;
            }
        }
        public uint HoleIndexLoc
        {
            get
            {
                return HoleIdxLoc;
            }
        }
        public uint HoleSize
        {
            get
            {
                return holesize;
            }
        }

        public DatHeader()
        {
            this.vmajor = 1;
            this.vmajor = 0;
            this.uvmajor = 0;
            this.uvminor = 0;
            this.indexvmajor = 7;
            this.entries = 0;
            this.IndexLoc = 96;
            this.indexsize = 0;
            this.holecount = 0;
            this.holesize = 0;
            this.HoleIdxLoc = 0;
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
            this.vmajor = br.ReadUInt32();
            this.vminor = br.ReadUInt32();
            this.uvmajor = br.ReadUInt32();
            this.uvminor = br.ReadUInt32();
            this.flags = br.ReadUInt32();
            this.datecreated = br.ReadUInt32();
            this.datemodified = br.ReadUInt32();
            this.indexvmajor = br.ReadUInt32();
            this.entries = br.ReadUInt32();
            this.IndexLoc = br.ReadUInt32();
            this.indexsize = br.ReadUInt32();
            this.holecount = br.ReadUInt32();
            this.HoleIdxLoc = br.ReadUInt32();
            this.holesize = br.ReadUInt32();
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
            bw.Write(this.vmajor);
            bw.Write(this.vminor);
            bw.Write(this.uvmajor);
            bw.Write(this.uvminor);
            bw.Write(this.flags);
            bw.Write(this.datecreated);
            bw.Write(this.datemodified);
            bw.Write(this.indexvmajor);
            bw.Write(this.entries);
            bw.Write(this.IndexLoc);
            bw.Write(this.indexsize);
            bw.Write(this.holecount);
            bw.Write(this.HoleIdxLoc);
            bw.Write(this.holesize);
            bw.Write(new byte[36]); // reserved byte padding
        }
    }

}
