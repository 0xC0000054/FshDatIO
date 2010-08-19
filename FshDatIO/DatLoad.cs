using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using FSHLib;
using System.Diagnostics;

namespace FshDatIO
{
    public class DatFile
    {
        DatHeader head = null;
        List<DatIndex> indexes = null;
        List<DirectoryEntry> dir = null;
        List<FshWrapper> files = null;

        string filename = string.Empty;
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

        public string Filename
        {
            get 
            {
                return filename;
            }
            set
            {
                filename = value;
            }
        }

        public DatHeader Header
        {
            get
            {
                return head;
            }
        }
        public List<DatIndex> Indexes
        {
            get 
            {
                return indexes;
            }
        }

        public bool Loaded
        {
            get
            {
                return loaded;
            }
        }

        public DatFile()
        {
            head = new DatHeader();
            indexes = new List<DatIndex>();
            files = new List<FshWrapper>();
            loaded = false;
            dirty = false;
        }

        public DatFile(string Filename)
        {
            reader = new BinaryReader(new FileStream(Filename, FileMode.Open, FileAccess.Read));
	        
            this.Load(reader);
            this.filename = Filename;
            this.dirty = false;
        }

        public void Load(BinaryReader br)
        { 
            head = new DatHeader(br);
            indexes = new List<DatIndex>();
            files = new List<FshWrapper>();
            
            br.BaseStream.Seek((long)head.IndexLocation, SeekOrigin.Begin);
            for (int i = 0; i < head.Entries; i++)
            {
                uint type = br.ReadUInt32();
                uint group = br.ReadUInt32();
                uint instance = br.ReadUInt32();
                uint location = br.ReadUInt32();
                uint size = br.ReadUInt32();

                if (type == 0xe86b1eef) // Compression Directory
                {
                    br.BaseStream.Seek((long)location, SeekOrigin.Begin);
                    dir = new List<DirectoryEntry>((int)(size / 16));
                    for (int d = 0; d < dir.Capacity; d++)
                    {
                        DirectoryEntry entry = new DirectoryEntry(br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32());
                        dir.Insert(d, entry);
                    }
                }      
                else if (type == 0x7ab50e44)
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

        public FshWrapper LoadFile(uint Group, uint Instance)
        {
            int idx = indexes.Find(0x7ab50e44, Group, Instance);

            if (idx == -1)  
                throw new DatFileException("The file at the specified index does not exist."); //  not a valid index so return null

            DatIndex index = indexes[idx];
            FshWrapper fsh = files.FromDatIndex(idx);
            if (fsh == null)
                throw new DatFileException(string.Format("Unable to find the Fsh file at index number {0}", idx.ToString()));

            if (!fsh.Loaded)
            {
                reader.BaseStream.Seek((long)index.Location, SeekOrigin.Begin);
                byte[] fshbuf = reader.ReadBytes((int)index.FileSize);
                
                bool comp = false;
                if ((fshbuf.Length > 5) && (fshbuf[4] == 0x10 && fshbuf[5] == 0xfb))
                {
                    comp = true;
                    using(MemoryStream ms = new MemoryStream(fshbuf))
	                {
                        fshbuf = QfsComp.Decomp(ms, 0, (int)ms.Length).ToArray(); 
	                }
                }
                fsh.Compressed = comp;
                fsh.Load(new MemoryStream(fshbuf));

                fsh.Loaded = true;
            }

            return fsh;
        }

        public void Close()
        {
            if (reader != null)
            {
                reader.Close();
                reader = null;
            }
        }

        public void Add(FshWrapper fshitem, uint Group, uint Instance, bool compress)
        {
            if (fshitem == null)
                throw new ArgumentNullException("fshitem", "fshitem is null.");

            fshitem.Compressed = compress;
            this.files.Add(fshitem);

            DatIndex addidx = new DatIndex(0x7ab50e44, Group, Instance) { Flags = DatIndexFlags.New };
            this.indexes.Add(addidx);

            this.dirty = true;
        }

        public void Remove(uint Group, uint Instance)
        {
            int idx = indexes.Find(0x7ab50e44, Group, Instance);
            if (idx != -1)
            {
                indexes[idx].Flags = DatIndexFlags.Deleted;
                this.dirty = true;
            }

        }

        public void Save()
        { 
            if (!string.IsNullOrEmpty(filename))
            {
                Save(filename);
            }
        }

        public void Save(string filename)
        {
            if (String.IsNullOrEmpty(filename))
                throw new ArgumentException("filename is null or empty.", "filename");

            if ((filename == this.Filename) && (reader != null))
            {
                this.Close();
            }

            using (BinaryWriter bw = new BinaryWriter(new FileStream(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite)))
            {
                DatHeader head = this.head;
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
                            if (reader != null)
                            {
                                reader.BaseStream.Seek((long)index.Location, SeekOrigin.Begin);
                                reader.BaseStream.Read(rawbuf, 0, (int)size);
                            }
                            else
                            {
                                bw.BaseStream.Seek((long)index.Location, SeekOrigin.Begin);
                                bw.BaseStream.Read(rawbuf, 0, (int)size);
                            }
#if DEBUG
                            Debug.WriteLine(string.Format("CompSig = {0}{1}", rawbuf[4].ToString("X"), rawbuf[5].ToString("X"))); 
#endif
                            bw.Write(rawbuf);

                            if ((rawbuf.Length > 5) && (rawbuf[4] == 0x10 && rawbuf[5] == 0xfb))
                            {
                                dirs.Add(new DirectoryEntry(index.Type, index.Group, index.Instance, (uint)size));
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

                this.head = head;
                this.indexes = saveindexes;
                this.dir = dirs;
                this.filename = filename;

                GC.Collect(1);

                this.dirty = false;
            }
        }

        /// <summary>
        /// Gets the currnt Unix Timestamp for the DatHeader DateCreated and DateModified Date stamps 
        /// </summary>
        /// <returns>The datestamp in Unix format</returns> 
        private static uint GetCurrentUnixTimestamp()
        {
            TimeSpan t = (DateTime.UtcNow - new DateTime(1970, 1, 1).ToUniversalTime());
            
            return (uint)t.TotalSeconds;
        }
    }
}
