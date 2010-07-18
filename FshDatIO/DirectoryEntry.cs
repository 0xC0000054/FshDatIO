using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace FshDatIO
{
    internal class DirectoryEntry
    {
        uint type;
        uint group;
        uint instance;
        uint size;

        public DirectoryEntry(uint Type, uint Group, uint Instance, uint Size)
        {
            this.type = Type;
            this.group = Group;
            this.instance = Instance;
            this.size = Size;
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

        public uint Size
        {
            get
            {
                return size;
            }
        }

        public void Save(BinaryWriter bw)
        {
            bw.Write(this.type);
            bw.Write(this.group);
            bw.Write(this.instance);
            bw.Write(this.size);
        }
    }

}
