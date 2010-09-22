using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;
using FSHLib;
using System.Drawing;
using System.Drawing.Drawing2D;
using FshDatIO;
using System.Globalization;

namespace readfshdat
{
    class Program
    {

        static void Main(string[] args)// savetest
        {
             DateTime unix = new DateTime(1970, 1, 1, 0, 0, 0);
             string dat = @"C:\Documents and Settings\Nicholas\My Documents\SimCity 4\Plugins\power station 1-0x5ad0e817_0x129a82f9_0x10000.SC4Model";
             Console.WriteLine(dat);
            DatFile dl = new DatFile(dat);
            try
            {
                int i = 0;
                foreach (var item in dl.Indexes)
                {
                    if (item.Type == 0x7ab50e44)
                    {
                        FshWrapper fw = dl.LoadFile(item.Group, item.Instance);
                        Debug.WriteLine(string.Format("Fsh {0} Width = {1}, Height = {2}",i++,((BitmapItem)fw.Image.Bitmaps[0]).Bitmap.Width,((BitmapItem)fw.Image.Bitmaps[0]).Bitmap.Height));
                    }
                }
                
                string savename = @"C:\Documents and Settings\Nicholas\Desktop\Problematic Images\temp2.dat";

                dl.Save(string.Empty);
            }
            catch (DatFileException dfx)
            {
                Console.WriteLine(dfx.Message);
            }
            catch (ArgumentException ax)
            {
                Console.WriteLine(ax.Message);
            }
            catch (Exception ex)
            {
                Console.Write(ex.ToString());
            }
           /* DatFile rel = new DatFile(savename);
            rel.Close();*/

            string path = @"C:\Program Files\Maxis\SimCity 4 Deluxe\SimCity_1.dat";

            using (BinaryReader br = new BinaryReader(new FileStream(path,FileMode.Open,FileAccess.Read)))
            {
                string filename = Path.GetFileName(((FileStream)br.BaseStream).Name);
                DatHeader head = new DatHeader(br);
                DateTime create = unix.AddSeconds(head.DateCreated);
                Console.WriteLine("{0} Created = {1}", filename, create.ToString(CultureInfo.CurrentCulture)); 
                DateTime mod = unix.AddSeconds(head.DateModified);
                Console.WriteLine("{0} Modified = {1}", filename, mod.ToString(CultureInfo.CurrentCulture)); 

            }

           /* using (MemoryStream ms = new MemoryStream())
            {
                fshw.Save(ms);
                byte[] data = QfsComp.Decomp(ms, 0, (int)ms.Length).ToArray();
            }*/
            /*byte[] rawfile = null;
            using (FileStream fs = new FileStream(@"C:\Documents and Settings\Nicholas\Desktop\Problematic Images\temp.dat", FileMode.Open, FileAccess.Read))
            {
                rawfile = new byte[(int)fs.Length];
                fs.Read(rawfile, 0, (int)fs.Length);
            }

            using (BinaryReader br = new BinaryReader(new MemoryStream(rawfile),Encoding.Default))
            {
                DatHeader head = new DatHeader(br);
                List<DatIndex> indexes = new List<DatIndex>();
                List<DirectoryEntry> dir = null;
                br.BaseStream.Seek((long)head.IndexLocation, SeekOrigin.Begin);
                for (int i = 0; i < head.Entries; i++)
                {
                    uint type = br.ReadUInt32();
                    uint group = br.ReadUInt32();
                    uint instance = br.ReadUInt32();
                    uint location = br.ReadUInt32();
                    uint size = br.ReadUInt32();
                    indexes.Add(new DatIndex(type,group,instance,location,size));
                    if (type == 0xe86b1eef)
                    {
                        br.BaseStream.Seek((long)location, SeekOrigin.Begin);
                        dir = new List<DirectoryEntry>((int)(size / 16));
                        for (int d = 0; d < dir.Capacity; d++)
                        {
                            DirectoryEntry entry = new DirectoryEntry(br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32());
                            dir.Insert(d, entry);
                        }
                        Console.WriteLine("Number of Compressed Files = {0}", dir.Count.ToString());
                    }
                }

                Debug.WriteLine(indexes.Count.ToString());
            }*/

            Console.WriteLine("\n Press any key to exit");
            Console.ReadLine();
        }
    }
}
