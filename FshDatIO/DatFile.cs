using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using FSHLib;
using System.Diagnostics;
using FshDatIO.Properties;
using System.Globalization;
using System.Collections.ObjectModel;
using System.Security.Permissions;

namespace FshDatIO
{
    /// <summary>
    /// Encapsulates a DBPF file.
    /// </summary>
    public sealed class DatFile : IDisposable
    {
        private DatHeader header;
        private List<DatIndex> indexes;
        private string datFileName;
        private bool loaded;
        private bool dirty;
        private BinaryReader reader;
        private const uint fshTypeId = 0x7ab50e44;
        /// <summary>
        /// The start date of unix time in UTC format
        /// </summary>
        private static readonly DateTime unixEpochUTC = new DateTime(1970, 1, 1).ToUniversalTime();

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
                return indexes.AsReadOnly();
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
            this.indexes = new List<DatIndex>();
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
            if (String.IsNullOrEmpty(fileName))
                throw new ArgumentException("Filename is null or empty.", "fileName");

            this.Load(new FileStream(fileName, FileMode.Open, FileAccess.Read));
            this.datFileName = fileName;
        }
        /// <summary>
        /// Loads a DatFile from the specified Stream
        /// </summary>
        /// <param name="input">The input stream to load from</param>
        /// <exception cref="System.ArgumentNullException">Thrown when the <see cref="Stream"></see> input is null</exception>
        /// <exception cref="FshDatIO.DatHeaderException">Thrown when the DatHeader identifier is invalid, does not equal DBPF.</exception>
        public void Load(Stream input)
        {
            if (input == null)
                throw new ArgumentNullException("input", "input is null.");

            if (reader != null)
                reader.Close();

            reader = new BinaryReader(input);

            this.header = new DatHeader(reader);

            int entryCount = (int)header.Entries;

            this.indexes = new List<DatIndex>(entryCount);

            reader.BaseStream.Seek((long)header.IndexLocation, SeekOrigin.Begin);
            for (int i = 0; i < entryCount; i++)
            {
                uint type = reader.ReadUInt32();
                uint group = reader.ReadUInt32();
                uint instance = reader.ReadUInt32();
                uint location = reader.ReadUInt32();
                uint size = reader.ReadUInt32();

                DatIndex index = new DatIndex(type, group, instance, location, size);
                if (type == fshTypeId) 
                {
                    index.FileItem = new FshWrapper();
                }
                indexes.Add(index); 
            }

            this.loaded = true;
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
        public FshWrapper LoadFile(uint group, uint instance)
        {
            if (this.disposed)
                throw new ObjectDisposedException("DatFile");

            int idx = indexes.Find(fshTypeId, group, instance);

            if (idx == -1)  
                throw new DatIndexException(Resources.SpecifiedIndexDoesNotExist); //  not a valid index so throw a DatFileException

            DatIndex index = indexes[idx];

            FshWrapper fsh = index.FileItem;
            if (fsh == null)
                throw new DatFileException(string.Format(CultureInfo.CurrentCulture, Resources.UnableToFindTheFshFileAtIndexNumber_Format, idx.ToString(CultureInfo.CurrentCulture)));

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
                throw new ObjectDisposedException("DatFile");

            if (index == null)
                throw new ArgumentNullException("index");

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
            int idx = indexes.Find(fshTypeId, group, instance);

            if (idx == -1)
                throw new DatIndexException(Resources.SpecifiedIndexDoesNotExist); //  not a valid index so throw a DatFileException

            return CheckImageSize(this.indexes[idx]);
        }

        /// <summary>
        /// Closes this instance.
        /// </summary>
        public void Close()
        {
            Dispose(true);
        }
        /// <summary>
        /// Add a FshWrapper item to the DatFile.
        /// </summary>
        /// <param name="fshItem">The FshWrapper to add to the DatFile.</param>
        /// <param name="group">The TGI group id of the FshWrapper item.</param>
        /// <param name="instance">The TGI instance id of the FshWrapper item.</param>
        /// <param name="compress">Compress the added FshWrapper item.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when the FshWrapper item is null</exception>
        public void Add(FshWrapper fshItem, uint group, uint instance, bool compress)
        {

            if (fshItem == null)
                throw new ArgumentNullException("fshItem");

            fshItem.Compressed = compress;
           
            DatIndex addidx = new DatIndex(fshTypeId, group, instance) { IndexState = DatIndexState.New, FileItem = fshItem };
            this.indexes.Add(addidx);

            this.dirty = true;
        }

        private int GetDeletedIndexCount()
        {
            int count = 0;
            foreach (var item in this.indexes)
            {
                if (item.IndexState == DatIndexState.Deleted || item.Type == 0xe86b1eef)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Removes the specified file from the DatFile
        /// </summary>
        /// <param name="group">The TGI group id to remove</param>
        /// <param name="instance">The TGI instance id to remove</param>
        /// <returns>The index of the removed file.</returns>
        public void Remove(uint group, uint instance)
        {
            int idx = indexes.Find(fshTypeId, group, instance);
            if (idx != -1)
            {
                this.indexes[idx].IndexState = DatIndexState.Deleted;
                this.dirty = true;
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
            if (String.IsNullOrEmpty(fileName))
                throw new ArgumentException("Filename is null or empty.", "fileName");

            if ((fileName == this.datFileName) && (this.reader != null))
            {
                this.reader.Close(); // if the fileName is the same close the BinaryReader
                this.reader = null;
            }

            FileStream fs = null;

            try
            {
                fs = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite);

                using (BinaryWriter bw = new BinaryWriter(fs))
                {               
                    fs = null;

                    DatHeader head = this.header;
                    head.DateCreated = GetCurrentUnixTimestamp();
                    head.Save(bw);
                    List<DatIndex> saveIndices = new List<DatIndex>(this.indexes.Count + 2);
                    List<DirectoryEntry> compDirs = new List<DirectoryEntry>();
                    uint location = 0;
                    uint size = 0;
                    
                    int indexCount = this.indexes.Count - GetDeletedIndexCount();
                    for (int i = 0; i < indexCount; i++)
                    {
                        DatIndex index = indexes[i];
                        if (index.IndexState != DatIndexState.Deleted && index.Type != 0xe86b1eef)
                        {
                            if (index.IndexState == DatIndexState.New && index.Type == fshTypeId)
                            {
                                FshWrapper fshw = index.FileItem;
#if DEBUG
                                Debug.WriteLine(string.Format("Item # {0} Instance = {1}\n", i.ToString(), index.Instance.ToString("X")));
#endif
                                if (fshw.Image != null)
                                {
                                    location = (uint)bw.BaseStream.Position;

                                    size = (uint)fshw.Save(bw.BaseStream);

                                    if (fshw.Image.IsCompressed)
                                    {
                                        compDirs.Add(new DirectoryEntry(index.Type, index.Group, index.Instance, (uint)fshw.Image.RawDataLength));
                                    } 
                                }
                                else
                                {
                                    continue; // skip any null images
                                }
                            }
                            else
                            {
                                location = (uint)bw.BaseStream.Position;
                                size = index.FileSize;

#if DEBUG
                                Debug.WriteLine(string.Format("Index {0} Type = {1} Compressed = {2}", i.ToString(), index.Type.ToString("X"), index.Compressed.ToString()));
#endif

                                byte[] rawbuf = new byte[size];
                                if (reader != null) // read from the original file if it is open
                                {
                                    reader.BaseStream.ProperRead(rawbuf, (int)index.Location, (int)size);
                                }
                                else
                                {
                                    // otherwise the original file should have the same name
                                    bw.BaseStream.ProperRead(rawbuf, (int)index.Location, (int)size);
                                }
                                if (bw.BaseStream.Position != location)
                                {
                                    bw.Seek((int)location, SeekOrigin.Begin);
                                }

#if DEBUG
                                Debug.WriteLine(string.Format("CompSig = {0}{1}", rawbuf[4].ToString("X"), rawbuf[5].ToString("X")));
#endif
                                bw.Write(rawbuf);

                                if ((rawbuf.Length > 5) && ((rawbuf[4] & 0xfe) == 0x10 && rawbuf[5] == 0xfb))
                                {
                                    compDirs.Add(new DirectoryEntry(index.Type, index.Group, index.Instance, size));
                                }
                            }
                            saveIndices.Add(new DatIndex(index.Type, index.Group, index.Instance, location, size));

                        }
                        else
                        {
                            indexes.Remove(index);
                            i--;
                        }
                    }

                    if (compDirs.Count > 0)
                    {
                        location = (uint)bw.BaseStream.Position;

                        int count = compDirs.Count;
                        for (int i = 0; i < count; i++)
                        {
                            compDirs[i].Save(bw);
                        }
                        
                        size = (uint)(compDirs.Count * 16);
                        saveIndices.Add(new DatIndex(0xe86b1eef, 0xe86b1eef, 0x286b1f03, location, size));
                    }

                    saveIndices.TrimExcess();

                    uint entryCount = (uint)saveIndices.Count;
                    location = (uint)bw.BaseStream.Position;
                    size = (entryCount * 20U);
                    int length = saveIndices.Count;
                    for (int i = 0; i < length; i++)
                    {
                        saveIndices[i].Save(bw);
                    }
                    head.Entries = entryCount;
                    head.IndexSize = size;
                    head.IndexLocation = location;
                    head.DateModified = GetCurrentUnixTimestamp();

                    bw.BaseStream.Position = 0L;
                    head.Save(bw);

                    this.header = head;
                    this.indexes = saveIndices;
                    this.datFileName = fileName;

                    this.dirty = false;
                }
                
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
        /// Gets the current Unix Timestamp for the DatHeader DateCreated and DateModified Date stamps 
        /// </summary>
        /// <returns>The datestamp in Unix format</returns> 
        private static uint GetCurrentUnixTimestamp()
        {
            TimeSpan t = (DateTime.UtcNow - unixEpochUTC);
            
            return (uint)t.TotalSeconds;
        }

        #region IDisposable Members

        private bool disposed;
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
                    if (reader != null)
                    {
                        reader.Close();
                        reader = null;
                    }


                    IEnumerable<DatIndex> files = this.indexes.Where(index => index.Type == fshTypeId);
                    
                    foreach (var item in files)
                    {
                        item.FileItem.Dispose();
                        item.FileItem = null;
                    }
                    this.loaded = false;
                    
                }
                disposed = true;
            }
        }

        #endregion
    }
}
