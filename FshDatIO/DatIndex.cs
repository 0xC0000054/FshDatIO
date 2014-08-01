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
        private uint type;
        private uint group;
        private uint instance;
        private uint location;
        private uint fileSize;
        private DatIndexState indexState;
        private FshFileItem fileItem;

        internal const uint SizeOf = 20U;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatIndex"/> class.
        /// </summary>
        internal DatIndex()
        {
            this.Type = 0;
            this.Group = 0;
            this.Instance = 0;
            this.Location = 0;
            this.FileSize = 0;
            this.IndexState = DatIndexState.None;
            this.FileItem = null;
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
            this.Type = type;
            this.Group = group;
            this.Instance = instance;
            this.Location = location;
            this.FileSize = fileSize;
            this.IndexState = DatIndexState.None;
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

            this.Type = type;
            this.Group = group;
            this.Instance = instance;
            this.Location = 0;
            this.FileSize = 0;
            this.IndexState = DatIndexState.New;
            this.FileItem = fileItem;
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

        /// <summary>
        /// Gets the type id of the index.
        /// </summary>
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

        /// <summary>
        /// Gets the group id of the index.
        /// </summary>
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

        /// <summary>
        /// Gets the instance id of the index.
        /// </summary>
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

        /// <summary>
        /// Gets the location of the file.
        /// </summary>
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
            private set
            {
                fileSize = value;
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
    }
}
