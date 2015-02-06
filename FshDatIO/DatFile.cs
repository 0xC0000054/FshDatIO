using System;
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
        private BinaryReader reader;
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
            this.reader = null;
        }

        /// <summary>
        /// Initializes a new instance of the DatFile class and loads the specified fileName.  
        /// </summary>
        /// <param name="fileName">The fileName to load.</param>
        /// <exception cref="System.ArgumentException">Thrown when the fileName is null or empty.</exception>
        /// <exception cref="FshDatIO.DatHeaderException">Thrown when the DatHeader identifier is invalid, does not equal DBPF.</exception>
        public DatFile(string fileName)
        {
            if (fileName == null)
            {
                throw new ArgumentNullException("fileName");
            }

            this.datFileName = fileName;
            this.dirty = false;
            this.loaded = false;
            this.reader = null;
            Load(fileName);
        }

        /// <summary>
        /// Loads a DatFile from the specified file
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when <param ref="path" /> is null</exception>
        /// <exception cref="FshDatIO.DatHeaderException">Thrown when the DatHeader identifier is invalid, does not equal DBPF.</exception>
        /// <exception cref="System.IO.FileNotFoundException">Thrown when the specified file does not exist.</exception>
        public void Load(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException("fileName");
            }

            if (this.reader != null)
            {
                this.reader.Close();
            }

            FileStream stream = null;

            try
            {
                stream = new FileStream(path, FileMode.Open, FileAccess.Read);
                this.reader = new BinaryReader(stream);
                stream = null;

                this.header = new DatHeader(reader);
                int entryCount = (int)header.Entries;

                this.indices = new DatIndexCollection(entryCount);

                this.reader.BaseStream.Seek((long)header.IndexLocation, SeekOrigin.Begin);
                for (int i = 0; i < entryCount; i++)
                {
                    uint type = reader.ReadUInt32();
                    uint group = reader.ReadUInt32();
                    uint instance = reader.ReadUInt32();
                    uint location = reader.ReadUInt32();
                    uint size = reader.ReadUInt32();

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
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (stream != null)
                {
                    stream.Dispose();
                    stream = null;
                }
            }
        }

        /// <summary>
        /// Loads a FshWrapper item from the DatFile.
        /// </summary>
        /// <param name="group">The TGI group id to load.</param>
        /// <param name="instance">The TGI instance id to load.</param>
        /// <returns>The loaded FshWrapper item</returns>
        /// <exception cref="FshDatIO.DatIndexException">Thrown when the specified index does not exist in the DatFile</exception>
        /// <exception cref="FshDatIO.DatFileException">Thrown when the Fsh file is not found at the specified index in the DatFile</exception>
        /// <exception cref="System.ObjectDisposedException">Thrown when the method is called after the DatFile has been closed.</exception>
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
                reader.BaseStream.Seek((long)index.Location, SeekOrigin.Begin);
                byte[] fshbuf = reader.ReadBytes((int)index.FileSize);

                fsh.Load(fshbuf);
            }

            return fsh;
        }

        /// <summary>
        /// Checks the size of the images within the fsh.
        /// </summary>
        /// <param name="index">The <see cref="FshDatIO.DatIndex"/> of the file to check.</param>
        /// <returns>True if all the image are at least 128x128; otherwise false.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown when the index is null.</exception>
        /// <exception cref="System.ObjectDisposedException">Thrown when the method is called after the DatFile has been closed.</exception>
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

            reader.BaseStream.Seek((long)index.Location, SeekOrigin.Begin);
            byte[] fshbuf = reader.ReadBytes((int)index.FileSize);

            return FSHImageWrapper.CheckImageSize(fshbuf);
        }

        /// <summary>
        /// Checks the size of the images within the fsh.
        /// </summary>
        /// <param name="group">The group id of the file.</param>
        /// <param name="instance">The instance id of the file.</param>
        /// <returns>True if all the image are at least 128x128; otherwise false.</returns>
        /// <exception cref="FshDatIO.DatIndexException">Thrown when the specified index does not exist in the DatFile.</exception>
        /// <exception cref="System.ObjectDisposedException">Thrown when the method is called after the DatFile has been closed.</exception>
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
        /// <exception cref="System.ArgumentNullException">Thrown when the FshFileItem is null</exception>
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
        /// <exception cref="System.ArgumentException">The fileName is null or empty.</exception>
        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        public void Save(string fileName)
        {
            if (fileName == null)
            {
                throw new ArgumentNullException("fileName");
            }

            string saveFileName = fileName;
            if (fileName == this.datFileName && this.reader != null)
            {
                // When overwriting an existing file, we save to a temporary file first and then use File.Copy to overwrite it if the save was successful.
                saveFileName = Path.GetTempFileName();
            }

            FileStream fs = null;

            try
            {
                fs = new FileStream(saveFileName, FileMode.OpenOrCreate, FileAccess.Write);

                using (BinaryWriter writer = new BinaryWriter(fs))
                {
                    fs = null;

                    DatHeader head = this.header;
                    head.DateCreated = GetCurrentUnixTimestamp();
                    head.Save(writer);

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
                                    location = writer.BaseStream.Position;
                                    size = fshw.Save(writer.BaseStream);
                                    if (fshw.Image.IsCompressed)
                                    {
                                        compDirs.Add(new DirectoryEntry(index.Type, index.Group, index.Instance, fshw.Image.RawDataLength));
                                    }
                                }
                            }
                        }
                        else
                        {
                            location = writer.BaseStream.Position;
                            size = index.FileSize;

#if DEBUG
                            System.Diagnostics.Debug.WriteLine(string.Format("Index: {0} Type: 0x{1:X8}", i, index.Type));
#endif

                            byte[] rawbuf = new byte[size];


                            reader.BaseStream.Seek((long)index.Location, SeekOrigin.Begin);
                            reader.BaseStream.ProperRead(rawbuf, 0, (int)size);


                            writer.Write(rawbuf);

                            if ((rawbuf.Length > 5) && ((rawbuf[0] & 0xfe) == 0x10 && rawbuf[1] == 0xFB || rawbuf[4] == 0x10 && rawbuf[5] == 0xFB))
                            {
                                compDirs.Add(new DirectoryEntry(index.Type, index.Group, index.Instance, size));
                            }
                        }

                        saveIndices.Add(new DatIndex(index.Type, index.Group, index.Instance, (uint)location, size));
                    }

                    if (compDirs.Count > 0)
                    {
                        location = writer.BaseStream.Position;

                        int count = compDirs.Count;
                        for (int i = 0; i < count; i++)
                        {
                            compDirs[i].Save(writer);
                        }

                        size = (uint)(compDirs.Count * DirectoryEntry.SizeOf);
                        saveIndices.Add(new DatIndex(CompressionDirectoryType, 0xe86b1eef, 0x286b1f03, (uint)location, size));
                    }

                    uint entryCount = (uint)saveIndices.Count;
                    location = writer.BaseStream.Position;
                    size = entryCount * DatIndex.SizeOf;
                    for (int i = 0; i < saveIndices.Count; i++)
                    {
                        saveIndices[i].Save(writer);
                    }
                    head.Entries = entryCount;
                    head.IndexSize = size;
                    head.IndexLocation = (uint)location;
                    head.DateModified = GetCurrentUnixTimestamp();

                    writer.BaseStream.Position = 0L;
                    head.Save(writer);

                    this.header = head;
                    this.indices = saveIndices;
                    this.datFileName = fileName;

                    this.dirty = false;
                }

                if (saveFileName != fileName)
                {
                    // Close the old file and copy the new file in its place.
                    this.reader.Close();
                    this.reader = null;
                    
                    File.Copy(saveFileName, fileName, true);
                    File.Delete(saveFileName);

                    // Open the new file to prevent a NullRefrenceException if LoadFile is called.
                    FileStream temp = null;
                    try
                    {
                        temp = new FileStream(fileName, FileMode.Open, FileAccess.Read);
                        this.reader = new BinaryReader(temp);
                        temp = null;
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                    finally
                    {
                        if (temp != null)
                        {
                            temp.Dispose();
                            temp = null;
                        }
                    }
                }

            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (fs != null)
                {
                    fs.Dispose();
                    fs = null;
                }
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
                    if (this.reader != null)
                    {
                        this.reader.Close();
                        this.reader = null;
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
