using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace FshDatIO
{
    public enum DatIndexState
    {
        None,
        New,
        Deleted
    }

    public sealed class DatIndex
    {
        private uint type;
        private uint group;
        private uint instance;
        private uint location;
        private uint fileSize;
        private DatIndexState indexState;
        private bool compressed; 

        public DatIndex()
        {
            this.Type = 0;
            this.Group = 0;
            this.Instance = 0;
            this.Location = 0;
            this.FileSize = 0;
            this.IndexState = DatIndexState.None;
            this.Compressed = false;
        }
        public DatIndex(uint type, uint group, uint instance, uint location, uint fileSize)
        {
            this.Type = type;
            this.Group = group;
            this.Instance = instance;
            this.Location = location;
            this.FileSize = fileSize;
            this.IndexState = DatIndexState.None;
            this.Compressed = false;
        }
        public DatIndex(uint type, uint group, uint instance)
        {
            this.Type = type;
            this.Group = group;
            this.Instance = instance;
            this.Location = 0;
            this.FileSize = 0;
            this.IndexState = DatIndexState.None;
            this.Compressed = false;
        }

        public void Save(BinaryWriter bw)
        {
            if (bw == null)
                throw new ArgumentNullException("bw", "bw is null.");
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

        public uint Location
        {
            get
            {
                return location;
            }
            private set
            {
                location = value;
            }
        }

        public uint FileSize
        {
            get
            {
                return fileSize;
            }
            private set
            {
                fileSize = value;
            }
        }

        public DatIndexState IndexState
        {
            get
            {
                return indexState;
            }
            set
            {
                indexState = value;
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

        public bool Equals(uint typeId, uint groupId, uint instanceId)
        {
            return ((this.type == typeId) && (this.group == groupId) && (this.instance == instanceId));
        }
    }
}
