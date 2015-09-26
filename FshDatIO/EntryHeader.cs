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
                return this.code;
            }
            internal set
            {
                this.code = value;
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
                return this.width;
            }
            internal set
            {
                this.width = value;
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
                return this.height;
            }
            internal set
            {
                this.height = value;
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
            return this.misc;
        }

        /// <summary>
        /// Sets the misc data.
        /// </summary>
        /// <param name="data">The data to set.</param>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> is null.</exception>
        internal void SetMiscData(ushort[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            this.misc = data;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EntryHeader"/> class.
        /// </summary>
        /// <param name="auxHeaderCode">The header code of the attachment.</param>
        internal EntryHeader(int auxHeaderCode)
        {
            this.code = auxHeaderCode;
            this.width = 0;
            this.height = 0;
            this.misc = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EntryHeader"/> class.
        /// </summary>
        /// <param name="rawData">The byte array containing the header.</param>
        /// <param name="offset">The offset of the header in <paramref name="rawData"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="rawData"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is invalid.</exception>
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

        /// <summary>
        /// Initializes a new instance of the <see cref="EntryHeader"/> class.
        /// </summary>
        /// <param name="format">The format of the entry.</param>
        /// <param name="width">The width of the entry.</param>
        /// <param name="height">The height of the entry.</param>
        /// <param name="misc">The misc data.</param>
        internal EntryHeader(FshImageFormat format, int width, int height, ushort[] misc)
        {
            this.code = (int)format;
            this.width = (ushort)width;
            this.height = (ushort)height;
            this.misc = misc;
        }

        /// <summary>
        /// Saves the <see cref="EntryHeader"/> to the specified stream.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> where the header will be saved..</param>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> is null.</exception>
        internal void Save(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            stream.WriteInt32(this.code);
            stream.WriteUInt16(this.width);
            stream.WriteUInt16(this.height);

            for (int i = 0; i < 4; i++)
            {
                stream.WriteUInt16(this.misc[i]);
            }
        }

    }
}
