using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace FshDatIO
{
    internal class DirectoryEntry
    {
        private uint type;
        private uint group;
        private uint instance;
        private uint size;

        public DirectoryEntry(uint type, uint group, uint instance, uint size)
        {
            this.Type = type;
            this.Group = group;
            this.Instance = instance;
            this.Size = size;
        }

        public uint Type
        {
            get
            {
                return type;
            }
            private set
            {
                type = value;
            }
        }

        public uint Group
        {
            get
            {
                return group;
            }
            private set
            {
                group = value;
            }
        }

        public uint Instance
        {
            get
            {
                return instance;
            }
            private set
            {
                instance = value;
            }
        }

        public uint Size
        {
            get
            {
                return size;
            }
            private set
            {
                size = value;
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
