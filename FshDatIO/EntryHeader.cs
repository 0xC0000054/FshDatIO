using System;
using System.IO;

namespace FshDatIO
{
    /// <summary>
    /// The header for the images within a fsh file. 
    /// </summary>
    public sealed class EntryHeader
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
            internal set
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
            internal set
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
            internal set
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
            internal set
            {
                misc = value;
            }
        }

        internal EntryHeader()
        {
            this.code = 0;
            this.width = 0;
            this.height = 0;
            this.misc = new ushort[4];
        }

        internal EntryHeader(FshImageFormat format, int width, int height, ushort[] misc)
        {
            this.code = (int)format;
            this.width = (ushort)width;
            this.height = (ushort)height;
            this.misc = misc;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EntryHeader"/> struct from the specified BinaryReader.
        /// </summary>
        /// <param name="br">The BinaryReader to read from.</param>
        internal EntryHeader(BinaryReader br)
        {
            if (br == null)
            {
                throw new ArgumentNullException("br");
            }

            this.code = br.ReadInt32();
            this.width = br.ReadUInt16();
            this.height = br.ReadUInt16();
            this.misc = new ushort[4];
            for (int i = 0; i < 4; i++)
            {
                this.misc[i] = br.ReadUInt16();
            }
        }

        internal void Save(Stream stream)
        {
            stream.WriteInt32(this.code);
            stream.WriteUInt16(this.width);
            stream.WriteUInt16(this.height);

            for (int i = 0; i < 4; i++)
            {
                stream.WriteUInt16(misc[i]);
            }
        }

    }
}
