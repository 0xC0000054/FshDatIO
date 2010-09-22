using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace FshDatIO
{
    public enum DatIndexFlags
    {
        None,
        New,
        Deleted
    }

    public class DatIndex
    {
        uint type;
        uint group;
        uint instance;
        uint location;
        uint fileSize;
        DatIndexFlags flags;
        bool compressed; 

        public DatIndex()
        {
            this.type = 0;
            this.group = 0;
            this.instance = 0;
            this.location = 0;
            this.fileSize = 0;
            this.flags = DatIndexFlags.None;
            this.compressed = false;
        }
        public DatIndex(uint Type, uint Group, uint Instance, uint Location, uint Filesize)
        {
            this.type = Type;
            this.group = Group;
            this.instance = Instance;
            this.location = Location;
            this.fileSize = Filesize;
            this.flags = DatIndexFlags.None;
            this.compressed = false;
        }
        public DatIndex(uint Type, uint Group, uint Instance)
        {
            this.type = Type;
            this.group = Group;
            this.instance = Instance;
            this.location = 0;
            this.fileSize = 0;
            this.flags = DatIndexFlags.None;
            this.compressed = false;
        }

        public void Save(BinaryWriter bw)
        {
            bw.Write(this.type);
            bw.Write(this.group);
            bw.Write(this.instance);
            bw.Write(this.location);
            bw.Write(this.fileSize);
        }

        public uint Type
        {
            get
            {
                return type;
            }
        }

        public uint Group
        {
            get
            {
                return group;
            }
        }

        public uint Instance
        {
            get
            {
                return instance;
            }
        }

        public uint Location
        {
            get
            {
                return location;
            }
        }

        public uint FileSize
        {
            get
            {
                return fileSize;
            }
        }

        public DatIndexFlags Flags
        {
            get
            {
                return flags;
            }
            set
            {
                flags = value;
            }
        }
        public bool Compressed
        {
            get
            {
                return compressed;
            }
            set
            {
                compressed = value;
            }
        }
    }
}
