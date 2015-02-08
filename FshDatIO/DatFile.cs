﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using FshDatIO.Properties;

namespace FshDatIO
{
    /// <summary>
    /// Encapsulates a DBPF file.
    /// </summary>
    public sealed class DatFile : IDisposable
    {
        private DatHeader header;
        private DatIndexCollection indices;
        private string datFileName;
        private bool loaded;
        private bool dirty;
        private Stream stream;
        private bool disposed;

        private const uint FshTypeID = 0x7ab50e44;
        private const uint CompressionDirectoryType = 0xe86b1eef;
        /// <summary>
        /// The start date of unix time in UTC format
        /// </summary>
        private static readonly DateTime UnixEpochUTC = new DateTime(1970, 1, 1).ToUniversalTime();

        /// <summary>
        /// Gets or sets a value indicating whether this instance is dirty.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is dirty; otherwise, <c>false</c>.
        /// </value>
        public bool IsDirty
        {
            get
            {
                return dirty;
            }
            set
            {
                dirty = value;
            }
        }


        /// <summary>
        /// Gets the name of the loaded file.
        /// </summary>
        /// <value>
        /// The name of the file.
        /// </value>
        public string FileName
        {
            get
            {
                return datFileName;
            }
        }

        /// <summary>
        /// Gets the <see cref="FshDatIO.DatHeader"/> of the DatFile.
        /// </summary>
        public DatHeader Header
        {
            get
            {
                return header;
            }
        }
        /// <summary>
        /// Gets the <see cref="FshDatIO.DatIndex"/> collection from the DatFile.
        /// </summary>
        public ReadOnlyCollection<DatIndex> Indexes
        {
            get
            {
                return indices.AsReadOnly();
            }
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="DatFile"/> is loaded.
        /// </summary>
        /// <value>
        ///   <c>true</c> if loaded; otherwise, <c>false</c>.
        /// </value>
        public bool Loaded
        {
            get
            {
                return loaded;
            }
        }

        /// <summary>
        /// Initializes a new instance of the DatFile class.  
        /// </summary>
        public DatFile()
        {
            this.header = new DatHeader();
            this.indices = new DatIndexCollection();
            this.datFileName = string.Empty;
            this.loaded = false;
            this.dirty = false;
            this.stream = null;
        }

        /// <summary>
        /// Initializes a new instance of the DatFile class and loads the specified path.  
        /// </summary>
        /// <param name="path">The path of the file to load.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="path"/> is null.</exception>
        /// <exception cref="DatFileException">The DBPF format version is not supported.</exception>
        /// <exception cref="DatFileException">The size of the index table is invalid.</exception>
        /// <exception cref="DatHeaderException">The header identifier does not equal DBPF.</exception>
        /// <exception cref="System.IO.FileNotFoundException">The file specified in <paramref name="path"/> was not found.</exception>
        public DatFile(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            this.header = null;
            this.indices = null;
            this.datFileName = path;
            this.dirty = false;
            this.loaded = false;
            this.stream = null;
            Load(path);
        }

        /// <summary>
        /// Loads a DatFile from the specified file
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <exception cref="System.ArgumentNullException"><param ref="path" /> is null</exception>
        /// <exception cref="DatFileException">The DBPF format version is not supported.</exception>
        /// <exception cref="DatFileException">The size of the index table is invalid.</exception>
        /// <exception cref="DatHeaderException">The header identifier does not equal DBPF.</exception>
        /// <exception cref="System.IO.FileNotFoundException">The file specified in <paramref name="path"/> was not found.</exception>
        private void Load(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            this.stream = new FileStream(path, FileMode.Open, FileAccess.Read);

            this.header = new DatHeader(stream);

            uint expectedIndexSize = this.header.Entries * DatIndex.SizeOf;
            if (this.header.IndexSize != expectedIndexSize)
            {
                throw new DatFileException(Resources.InvalidIndexTableSize);
            }

            int entryCount = (int)header.Entries;

            this.indices = new DatIndexCollection(entryCount);

            this.stream.Seek((long)header.IndexLocation, SeekOrigin.Begin);
            for (int i = 0; i < entryCount; i++)
            {
                uint type = stream.ReadUInt32();
                uint group = stream.ReadUInt32();
                uint instance = stream.ReadUInt32();
                uint location = stream.ReadUInt32();
                uint size = stream.ReadUInt32();

                DatIndex index = new DatIndex(type, group, instance, location, size);
                if (type == FshTypeID)
                {
                    index.FileItem = new FshFileItem();
                }
                this.indices.Add(index);
            }

            this.indices.SortByLocation();

            this.loaded = true;
        }

        /// <summary>
        /// Loads a FshWrapper item from the DatFile.
        /// </summary>
        /// <param name="group">The TGI group id to load.</param>
        /// <param name="instance">The TGI instance id to load.</param>
        /// <returns>The loaded FshWrapper item</returns>
        /// <exception cref="DatIndexException">The specified index does not exist in the DatFile</exception>
        /// <exception cref="DatFileException">The Fsh file is not found at the specified index in the DatFile</exception>
        /// <exception cref="System.ObjectDisposedException">The method is called after the DatFile has been disposed.</exception>
        public FshFileItem LoadFile(uint group, uint instance)
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException("DatFile");
            }

            DatIndex index = this.indices.Find(FshTypeID, group, instance);

            if (index == null)
            {
                throw new DatIndexException(Resources.SpecifiedIndexDoesNotExist);
            }

            FshFileItem fsh = index.FileItem;

            if (!fsh.Loaded)
            {
                this.stream.Seek((long)index.Location, SeekOrigin.Begin);
                byte[] imageData = this.stream.ReadBytes((int)index.FileSize);

                fsh.Load(imageData);
            }

            return fsh;
        }

        /// <summary>
        /// Checks the size of the images within the fsh.
        /// </summary>
        /// <param name="index">The <see cref="FshDatIO.DatIndex"/> of the file to check.</param>
        /// <returns><c>true</c> if all the image are at least 128x128; otherwise <c>false</c>.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="index"/> is null.</exception>
        /// <exception cref="System.ObjectDisposedException">The method is called after the DatFile has been closed.</exception>
        public bool CheckImageSize(DatIndex index)
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException("DatFile");
            }

            if (index == null)
            {
                throw new ArgumentNullException("index");
            }

            if (index.Type != FshTypeID)
            {
                return false;
            }

            this.stream.Seek((long)index.Location, SeekOrigin.Begin);
            byte[] imageBytes = this.stream.ReadBytes((int)index.FileSize);

            return FSHImageWrapper.CheckImageSize(imageBytes);
        }

        /// <summary>
        /// Checks the size of the images within the fsh.
        /// </summary>
        /// <param name="group">The group id of the file.</param>
        /// <param name="instance">The instance id of the file.</param>
        /// <returns>True if all the image are at least 128x128; otherwise false.</returns>
        /// <exception cref="FshDatIO.DatIndexException">The specified index does not exist in the DatFile.</exception>
        /// <exception cref="System.ObjectDisposedException">The method is called after the DatFile has been closed.</exception>
        public bool CheckImageSize(uint group, uint instance)
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException("DatFile");
            }

            DatIndex index = this.indices.Find(FshTypeID, group, instance);

            if (index == null)
            {
                throw new DatIndexException(Resources.SpecifiedIndexDoesNotExist);
            }

            return CheckImageSize(index);
        }

        /// <summary>
        /// Closes the current DatFile and the underlying stream.
        /// </summary>
        public void Close()
        {
            Dispose(true);
        }

        /// <summary>
        /// Add a FshFileItem item to the DatFile.
        /// </summary>
        /// <param name="fshItem">The FshWrapper to add to the DatFile.</param>
        /// <param name="group">The TGI group id of the FshWrapper item.</param>
        /// <param name="instance">The TGI instance id of the FshWrapper item.</param>
        /// <param name="compress">Compress the added FshWrapper item.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="fshItem"/> is null</exception>
        public void Add(FshFileItem fshItem, uint group, uint instance, bool compress)
        {
            if (fshItem == null)
            {
                throw new ArgumentNullException("fshItem");
            }

            fshItem.Compressed = compress;

            this.indices.Add(new DatIndex(FshTypeID, group, instance, fshItem));

            this.dirty = true;
        }

        /// <summary>
        /// Removes the specified file from the DatFile
        /// </summary>
        /// <param name="group">The TGI group id to remove</param>
        /// <param name="instance">The TGI instance id to remove</param>
        public void Remove(uint group, uint instance)
        {
            int index = this.indices.IndexOf(FshTypeID, group, instance);
            if (index != -1)
            {
                // Loop to remove any additional files with the same TGI, this should never happen but check anyway.
                do
                {
                    this.indices[index].IndexState = DatIndexState.Deleted;
                    index = this.indices.IndexOf(FshTypeID, group, instance, index + 1);
                }
                while (index >= 0);

                this.dirty = true;
            }
        }

        /// <summary>
        /// Trims the deleted items from the index collection.
        /// </summary>
        private void TrimDeletedItems()
        {
            if (this.indices.Count > 0)
            {
                this.indices.RemoveAll(new Predicate<DatIndex>(index => (index.IndexState == DatIndexState.Deleted || index.Type == CompressionDirectoryType)));
            }
        }

        /// <summary>
        /// Saves the currently loaded DatFile
        /// </summary>
        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        public void Save()
        {
            if (!string.IsNullOrEmpty(this.datFileName))
            {
                Save(this.datFileName);
            }
        }

        /// <summary>
        /// Saves the DatFile to the specified fileName.
        /// </summary>
        /// <param name="fileName">The fileName to save as</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="fileName"/> is null.</exception>
        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        public void Save(string fileName)
        {
            if (fileName == null)
            {
                throw new ArgumentNullException("fileName");
            }

            string saveFileName = fileName;
            if (fileName == this.datFileName && this.stream != null)
            {
                // When overwriting an existing file, we save to a temporary file first and then use File.Copy to overwrite it if the save was successful.
                saveFileName = Path.GetTempFileName();
            }

            using (FileStream output = new FileStream(saveFileName, FileMode.OpenOrCreate, FileAccess.Write))
            {
                DatHeader head = this.header;
                head.DateCreated = GetCurrentUnixTimestamp();
                head.Save(output);

                TrimDeletedItems();

                DatIndexCollection saveIndices = new DatIndexCollection(this.indices.Count + 2);
                List<DirectoryEntry> compDirs = new List<DirectoryEntry>();
                long location = 0;
                uint size = 0;

                for (int i = 0; i < this.indices.Count; i++)
                {
                    DatIndex index = indices[i];

                    if (index.IndexState == DatIndexState.New)
                    {
                        if (index.Type == FshTypeID)
                        {
                            FshFileItem fshw = index.FileItem;
#if DEBUG
                            System.Diagnostics.Debug.WriteLine(string.Format("Item # {0} Instance = {1}\n", i.ToString(), index.Instance.ToString("X")));
#endif
                            if (fshw.Image != null)
                            {
                                location = output.Position;
                                size = fshw.Save(output);
                                if (fshw.Image.IsCompressed)
                                {
                                    compDirs.Add(new DirectoryEntry(index.Type, index.Group, index.Instance, fshw.Image.RawDataLength));
                                }
                            }
                        }
                    }
                    else
                    {
                        location = output.Position;
                        size = index.FileSize;

#if DEBUG
                        System.Diagnostics.Debug.WriteLine(string.Format("Index: {0} Type: 0x{1:X8}", i, index.Type));
#endif

                        this.stream.Seek((long)index.Location, SeekOrigin.Begin);

                        int dataSize = (int)size;
                        byte[] buffer = this.stream.ReadBytes(dataSize);

                        output.Write(buffer, 0, dataSize);

                        if ((buffer.Length > 5) && ((buffer[0] & 0xfe) == 0x10 && buffer[1] == 0xFB || buffer[4] == 0x10 && buffer[5] == 0xFB))
                        {
                            compDirs.Add(new DirectoryEntry(index.Type, index.Group, index.Instance, size));
                        }
                    }

                    saveIndices.Add(new DatIndex(index.Type, index.Group, index.Instance, (uint)location, size));
                }

                if (compDirs.Count > 0)
                {
                    location = output.Position;

                    int count = compDirs.Count;
                    for (int i = 0; i < count; i++)
                    {
                        compDirs[i].Save(output);
                    }

                    size = (uint)(compDirs.Count * DirectoryEntry.SizeOf);
                    saveIndices.Add(new DatIndex(CompressionDirectoryType, 0xe86b1eef, 0x286b1f03, (uint)location, size));
                }

                uint entryCount = (uint)saveIndices.Count;
                location = output.Position;
                size = entryCount * DatIndex.SizeOf;
                for (int i = 0; i < saveIndices.Count; i++)
                {
                    saveIndices[i].Save(output);
                }
                head.Entries = entryCount;
                head.IndexSize = size;
                head.IndexLocation = (uint)location;
                head.DateModified = GetCurrentUnixTimestamp();

                output.Position = 0L;
                head.Save(output);

                this.header = head;
                this.indices = saveIndices;
                this.datFileName = fileName;

                this.dirty = false;
            }

            if (saveFileName != fileName)
            {
                // Close the old file and copy the new file in its place.
                this.stream.Dispose();
                this.stream = null;

                File.Copy(saveFileName, fileName, true);
                File.Delete(saveFileName);

                // Open the new file to prevent a NullRefrenceException if LoadFile is called.
                this.stream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            }
        }

        /// <summary>
        /// Gets the current Unix Timestamp for the DatHeader DateCreated and DateModified fields. 
        /// </summary>
        /// <returns>The timestamp in Unix format</returns> 
        private static uint GetCurrentUnixTimestamp()
        {
            TimeSpan t = (DateTime.UtcNow - UnixEpochUTC);

            return (uint)t.TotalSeconds;
        }

        #region IDisposable Members

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (this.stream != null)
                    {
                        this.stream.Dispose();
                        this.stream = null;
                    }

                    if (this.indices != null)
                    {
                        this.indices.Dispose();
                        this.indices = null;
                    }

                    this.loaded = false;

                }
                disposed = true;
            }
        }

        #endregion
    }
}
