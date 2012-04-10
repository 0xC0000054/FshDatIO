using System;

namespace FshDatIO
{
    /// <summary>
    /// The header for the images within a fsh file. 
    /// </summary>
    public struct EntryHeader
    {
        private int code;
        private ushort width;
        private ushort height;
        private ushort[] misc;

        /// <summary>
        /// Gets or sets the entry code.
        /// </summary>
        /// <value>
        /// The code.
        /// </value>
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

        /// <summary>
        /// Gets or sets the entry width.
        /// </summary>
        /// <value>
        /// The width.
        /// </value>
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

        /// <summary>
        /// Gets or sets the entry height.
        /// </summary>
        /// <value>
        /// The height.
        /// </value>
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

        /// <summary>
        /// Gets or sets the miscellaneous entry data.
        /// </summary>
        /// <value>
        /// The misc.
        /// </value>
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
        internal EntryHeader(System.IO.BinaryReader br)
        {
            if (br == null)
            {
                throw new ArgumentNullException("br");
            }

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
