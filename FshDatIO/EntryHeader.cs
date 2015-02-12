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

        internal const int SizeOf = 16;

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
        /// Gets the miscellaneous entry data.
        /// </summary>
        /// <value>
        /// The misc.
        /// </value>
        public ushort[] GetMiscData()
        {
            return misc;
        }

        internal void SetMiscData(ushort[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            this.misc = data;
        }

        internal EntryHeader(int auxHeaderCode)
        {
            this.code = auxHeaderCode;
            this.width = 0;
            this.height = 0;
            this.misc = null;
        }
        
        internal EntryHeader(byte[] rawData, int offset)
        {
            if (rawData == null)
            {
                throw new ArgumentNullException("rawData");
            }

            if (offset < 0 || offset > (rawData.Length - EntryHeader.SizeOf))
            {
                throw new ArgumentOutOfRangeException("offset");
            }

            this.code = LittleEndianBitConverter.ToInt32(rawData, offset);
            this.width = LittleEndianBitConverter.ToUInt16(rawData, offset + 4);
            this.height = LittleEndianBitConverter.ToUInt16(rawData, offset + 6);
            this.misc = new ushort[4];

            int miscOffset = offset + 8;
            for (int i = 0; i < this.misc.Length; i++)
            {
                this.misc[i] = LittleEndianBitConverter.ToUInt16(rawData, miscOffset);
                miscOffset += 2;
            }
        }

        internal EntryHeader(FshImageFormat format, int width, int height, ushort[] misc)
        {
            this.code = (int)format;
            this.width = (ushort)width;
            this.height = (ushort)height;
            this.misc = misc;
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
