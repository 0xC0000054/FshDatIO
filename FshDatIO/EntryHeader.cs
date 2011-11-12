using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FshDatIO
{
    public struct EntryHeader
    {
        private int code;
        private ushort width;
        private ushort height;
        private ushort[] misc;

        public int Code
        {
            get
            {
                return code;
            }
            set
            {
                code = value;
            }
        }

        public ushort Width
        {
            get
            {
                return width;
            }
            set
            {
                width = value;
            }
        }

        public ushort Height
        {
            get
            {
                return height;
            }
            set
            {
                height = value;
            }
        }

        public ushort[] Misc
        {
            get
            {
                return misc;
            }
            set
            {
                misc = value;
            }
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="EntryHeader"/> struct from the specified BinaryReader.
        /// </summary>
        /// <param name="br">The BinaryReader to read from.</param>
        public EntryHeader(System.IO.BinaryReader br)
        {
            this.code = br.ReadInt32();
            this.width = br.ReadUInt16();
            this.height = br.ReadUInt16();
            this.misc = new ushort[4];
            for (int m = 0; m < 4; m++)
            {
                this.misc[m] = br.ReadUInt16();
            }
        }

    }
}
