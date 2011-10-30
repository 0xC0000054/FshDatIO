using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using FSHLib;
using System.Diagnostics;
using FshDatIO.Properties;
using System.Globalization;
using System.Collections.ObjectModel;

namespace FshDatIO
{
    public sealed class DatFile : IDisposable
    {
        DatHeader header = null;
        List<DatIndex> indexes = null;
        List<DirectoryEntry> dir = null;
        List<FshWrapper> files = null;

        string fileName = string.Empty;
        bool loaded = false;
        bool dirty = false;

        BinaryReader reader = null;

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

        public string FileName
        {
            get 
            {
                return fileName;
            }
            set
            {
                if (System.String.IsNullOrEmpty(value))
                    throw new ArgumentException("value is null or empty.", "value");
                fileName = value;
            }
        }

        public DatHeader Header
        {
            get
            {
                return header;
            }
        }
        public ReadOnlyCollection<DatIndex> Indexes
        {
            get 
            {
                return indexes.AsReadOnly();
            }
        }

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
            header = new DatHeader();
            indexes = new List<DatIndex>();
            files = new List<FshWrapper>();
        }
        /// <summary>
        /// Initializes a new instance of the DatFile class and loads the specified fileName.  
        /// </summary>
        /// <param name="fileName">The fileName to load.</param>
        public DatFile(string fileName)
        {
            if (String.IsNullOrEmpty(fileName))
                throw new ArgumentException("Filename is null or empty.", "fileName");

            this.Load(new FileStream(fileName, FileMode.Open, FileAccess.Read));
            this.fileName = fileName;
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
            this.indexes = new List<DatIndex>();
            this.files = new List<FshWrapper>();

            reader.BaseStream.Seek((long)header.IndexLocation, SeekOrigin.Begin);
            for (int i = 0; i < header.Entries; i++)
            {
                uint type = reader.ReadUInt32();
                uint group = reader.ReadUInt32();
                uint instance = reader.ReadUInt32();
                uint location = reader.ReadUInt32();
                uint size = reader.ReadUInt32();

                if (type == 0xe86b1eef) // Compression Directory
                {
                    reader.BaseStream.Seek((long)location, SeekOrigin.Begin);
                    this.dir = new List<DirectoryEntry>((int)(size / 16));
                    for (int d = 0; d < dir.Capacity; d++)
                    {
                        DirectoryEntry entry = new DirectoryEntry(reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadUInt32());
                        dir.Insert(d, entry);
                    }
                }
                else if (type == 0x7ab50e44) // Fsh image
                {
                    files.Add(new FshWrapper() { FileIndex = i });
                }
                indexes.Add(new DatIndex(type, group, instance, location, size));

            }
            if ((dir != null) && dir.Count > 0)
            {
                for (int i = 0; i < indexes.Count; i++)
                {
                    DatIndex di = indexes[i];
                    if (di.Type != 0xe86b1eef)
                    {
                        int idx = dir.Find(di.Type, di.Group, di.Instance);
                        if (idx != -1)
                        {
                            indexes[i].Compressed = true;
                        }
                    }
                }
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
        public FshWrapper LoadFile(uint group, uint instance)
        {
            int idx = indexes.Find(0x7ab50e44, group, instance);

            if (idx == -1)  
                throw new DatIndexException(Resources.SpecifiedIndexDoesNotExist); //  not a valid index so throw a DatFileException

            DatIndex index = indexes[idx];
            FshWrapper fsh = files.FromDatIndex(idx);
            if (fsh == null)
                throw new DatFileException(string.Format(CultureInfo.CurrentCulture, Resources.UnableToFindTheFshFileAtIndexNumber_Format, idx.ToString(CultureInfo.CurrentCulture)));

            if (!fsh.Loaded)
            {
                reader.BaseStream.Seek((long)index.Location, SeekOrigin.Begin);
                byte[] fshbuf = reader.ReadBytes((int)index.FileSize);

                bool comp = false;
                if ((fshbuf.Length > 5) && (fshbuf[4] == 0x10 && fshbuf[5] == 0xfb))
                {
                    comp = true;
                    using (MemoryStream ms = new MemoryStream(fshbuf))
                    {
                        fshbuf = QfsComp.Decomp(ms, 0, (int)ms.Length);
                    }
                }
                fsh.Compressed = comp;
                using (MemoryStream ms = new MemoryStream(fshbuf))
                {
                    fsh.Load(ms);
                }
            }

            return fsh;
        }

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
                throw new ArgumentNullException("fshItem", "fshItem is null.");

            fshItem.Compressed = compress;
            this.files.Add(fshItem);

            DatIndex addidx = new DatIndex(0x7ab50e44, group, instance) { Flags = DatIndexFlags.New };
            this.indexes.Add(addidx);

            this.dirty = true;
        }
        /// <summary>
        /// Removes the specified file from the DatFile
        /// </summary>
        /// <param name="group">The TGI group id to remove</param>
        /// <param name="instance">The TGI instance id to remove</param>
        public void Remove(uint group, uint instance)
        {
            int idx = indexes.Find(0x7ab50e44, group, instance);
            if (idx != -1)
            {
                this.indexes[idx].Flags = DatIndexFlags.Deleted;
                this.dirty = true;
            }

        }
        /// <summary>
        /// Saves the currently loaded DatFile
        /// </summary>
        [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.LinkDemand, Flags = System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
        public void Save()
        { 
            if (!string.IsNullOrEmpty(fileName))
            {
                Save(fileName);
            }
        }
        /// <summary>
        /// Saves the DatFile to the specified fileName.
        /// </summary>
        /// <param name="fileName">The fileName to save as</param>
        /// <exception cref="System.ArgumentException">The fileName is null or empty.</exception>
        [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.LinkDemand, Flags = System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
        public void Save(string fileName)
        {
            if (String.IsNullOrEmpty(fileName))
                throw new ArgumentException("Filename is null or empty.", "fileName");

            if ((fileName == this.fileName) && (reader != null))
            {
                this.Close(); // if the fileName is the same close the file
            }
            using (FileStream fs = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    DatHeader head = this.header;
                    head.DateCreated = GetCurrentUnixTimestamp();
                    head.Save(bw);
                    List<DatIndex> saveindexes = new List<DatIndex>((this.indexes.Count + 2));
                    List<DirectoryEntry> dirs = new List<DirectoryEntry>();
                    uint location = 0;
                    uint size = 0;
                    for (int i = 0; i < this.indexes.Count; i++)
                    {
                        DatIndex index = indexes[i];
                        if (index.Flags != DatIndexFlags.Deleted && index.Type != 0xe86b1eef)
                        {
                            if (index.Flags == DatIndexFlags.New && index.Type == 0x7ab50e44)
                            {
                                FshWrapper fshw = files[i];
#if DEBUG
                                Debug.WriteLine(string.Format("Item # {0} Instance = {1}\n", i.ToString(), index.Instance.ToString("X")));
#endif
                                int rawlen = fshw.Image.RawData.Length;

                                location = (uint)bw.BaseStream.Position;

                                size = (uint)fshw.Save(bw.BaseStream);

                                if (fshw.Image.IsCompressed)
                                {
                                    dirs.Add(new DirectoryEntry(index.Type, index.Group, index.Instance, (uint)rawlen));
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
                                    reader.BaseStream.Seek((long)index.Location, SeekOrigin.Begin);
                                    reader.BaseStream.Read(rawbuf, 0, (int)size);
                                }
                                else
                                {
                                    // otherwise the original file should have the same name
                                    bw.BaseStream.Seek((long)index.Location, SeekOrigin.Begin);
                                    bw.BaseStream.Read(rawbuf, 0, (int)size);
                                }
#if DEBUG
                                Debug.WriteLine(string.Format("CompSig = {0}{1}", rawbuf[4].ToString("X"), rawbuf[5].ToString("X")));
#endif
                                bw.Write(rawbuf);

                                if ((rawbuf.Length > 5) && (rawbuf[4] == 0x10 && rawbuf[5] == 0xfb))
                                {
                                    dirs.Add(new DirectoryEntry(index.Type, index.Group, index.Instance, size));
                                }
                            }
                            saveindexes.Add(new DatIndex(index.Type, index.Group, index.Instance, location, size));

                        }
                        else
                        {
                            indexes.Remove(index);
                            i--;
                        }
                    }

                    if (dirs.Count > 0)
                    {
                        location = (uint)bw.BaseStream.Position;
                        for (int i = 0; i < dirs.Count; i++)
                        {
                            dirs[i].Save(bw);
                        }
                        size = (uint)(dirs.Count * 16);
                        saveindexes.Add(new DatIndex(0xe86b1eef, 0xe86b1eef, 0x286b1f03, location, size));
                    }

                    saveindexes.TrimExcess();

                    location = (uint)bw.BaseStream.Position;
                    size = (uint)(saveindexes.Count * 20);
                    uint entrycnt = (uint)saveindexes.Count;
                    for (int i = 0; i < saveindexes.Count; i++)
                    {
                        saveindexes[i].Save(bw);
                    }
                    head.Entries = entrycnt;
                    head.IndexSize = size;
                    head.IndexLocation = location;
                    head.DateModified = GetCurrentUnixTimestamp();

                    bw.BaseStream.Position = 0L;
                    head.Save(bw);

                    this.header = head;
                    this.indexes = saveindexes;
                    this.dir = dirs;
                    this.fileName = fileName;

                    this.dirty = false;
                }
            }
        }

        /// <summary>
        /// Gets the current Unix Timestamp for the DatHeader DateCreated and DateModified Date stamps 
        /// </summary>
        /// <returns>The datestamp in Unix format</returns> 
        private static uint GetCurrentUnixTimestamp()
        {
            TimeSpan t = (DateTime.UtcNow - new DateTime(1970, 1, 1).ToUniversalTime());
            
            return (uint)t.TotalSeconds;
        }

        #region IDisposable Members

        private bool disposed;
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
                }
                disposed = true;
            }
        }

        #endregion
    }
}
