using System;
using System.IO;

namespace FshDatIO
{
    /// <summary>
    /// The enum specifing the DatIndex states.
    /// </summary>
    public enum DatIndexState
    {
        /// <summary>
        /// The normal state of a DatIndex.
        /// </summary>
        None,
        /// <summary>
        /// The DatIndex contains a new file to add to the dat.
        /// </summary>
        New,
        /// <summary>
        /// The DatIndex will be deleted on save.
        /// </summary>
        Deleted
    }

    /// <summary>
    /// The class that holds the TGI and location data of an entry within the DatFile 
    /// </summary>
    public sealed class DatIndex
    {
        private readonly uint type;
        private readonly uint group;
        private readonly uint instance;
        private readonly uint location;
        private readonly uint fileSize;
        private DatIndexState indexState;
        private FshFileItem fileItem;

        internal const uint SizeOf = 20U;

        
        /// <summary>
        /// Gets the type id of the index.
        /// </summary>
        public uint Type
        {
            get
            {
                return type;
            }
        }

        /// <summary>
        /// Gets the group id of the index.
        /// </summary>
        public uint Group
        {
            get
            {
                return group;
            }
        }

        /// <summary>
        /// Gets the instance id of the index.
        /// </summary>
        public uint Instance
        {
            get
            {
                return instance;
            }
        }

        /// <summary>
        /// Gets the location of the file.
        /// </summary>
        public uint Location
        {
            get
            {
                return location;
            }
        }

        /// <summary>
        /// Gets the size of the file.
        /// </summary>
        /// <value>
        /// The size of the file.
        /// </value>
        public uint FileSize
        {
            get
            {
                return fileSize;
            }
        }

        /// <summary>
        /// Gets or sets the state of the index.
        /// </summary>
        /// <value>
        /// The state of the index.
        /// </value>
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

        /// <summary>
        /// Gets or sets the file item.
        /// </summary>
        /// <value>
        /// The file item.
        /// </value>
        public FshFileItem FileItem
        {
            get
            {
                return fileItem;
            }
            set
            {
                fileItem = value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DatIndex"/> class.
        /// </summary>
        internal DatIndex()
        {
            this.type = 0;
            this.group = 0;
            this.instance = 0;
            this.location = 0;
            this.fileSize = 0;
            this.indexState = DatIndexState.None;
            this.fileItem = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DatIndex"/> class.
        /// </summary>
        /// <param name="type">The type id of the entry.</param>
        /// <param name="group">The group id of the entry.</param>
        /// <param name="instance">The instance id of the entry.</param>
        /// <param name="location">The location of the entry.</param>
        /// <param name="fileSize">Size of the entry.</param>
        internal DatIndex(uint type, uint group, uint instance, uint location, uint fileSize)
        {
            this.type = type;
            this.group = group;
            this.instance = instance;
            this.location = location;
            this.fileSize = fileSize;
            this.indexState = DatIndexState.None;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DatIndex" /> class.
        /// </summary>
        /// <param name="type">The type id of the entry.</param>
        /// <param name="group">The group id of the entry.</param>
        /// <param name="instance">The instance id of the entry.</param>
        /// <param name="fileItem">The file item.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="fileItem"/> is null.</exception>
        internal DatIndex(uint type, uint group, uint instance, FshFileItem fileItem)
        {
            if (fileItem == null)
            {
                throw new ArgumentNullException("fileItem");
            }

            this.type = type;
            this.group = group;
            this.instance = instance;
            this.location = 0;
            this.fileSize = 0;
            this.indexState = DatIndexState.New;
            this.fileItem = fileItem;
        }

        /// <summary>
        /// Saves the DatIndex instance to the specified BinaryWriter.
        /// </summary>
        /// <param name="bw">The <see cref="System.IO.BinaryWriter"/> to save to.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when the BinaryWriter is null.</exception>
        internal void Save(BinaryWriter bw)
        {
            if (bw == null)
            {
                throw new ArgumentNullException("bw", "bw is null.");
            }

            bw.Write(this.type);
            bw.Write(this.group);
            bw.Write(this.instance);
            bw.Write(this.location);
            bw.Write(this.fileSize);
        }

    }
}
