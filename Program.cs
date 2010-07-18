using System;
using System.Collections.Generic;
using System.Text;
/*using SynapticEffect.SimCity.IO;
using SynapticEffect.SimCity.IO.FileTypes;
using SynapticEffect.SimCity.DatNamespace;*/
using System.Diagnostics;
using System.IO;
using FSHLib;
using System.Drawing;
using System.Drawing.Drawing2D;
using FshDatIO;

namespace readfshdat
{
    class Program
    {



        static void Main(string[] args)// savetest
        {
             DateTime unix = new DateTime(1970, 1, 1, 0, 0, 0);
            /*DatFile dl = new DatFile(@"C:\Documents and Settings\Nicholas\Desktop\Problematic Images\xpcomp.dat");

            FshWrapper fshw = dl.LoadFile(0x2bd684e4, 0x03aa5024);
            string savename = @"C:\Documents and Settings\Nicholas\Desktop\Problematic Images\temp2.dat";
            dl.Save(savename);

            DatFile rel = new DatFile(savename);
            rel.Close();*/

            string path = @"C:\Program Files\Maxis\SimCity 4 Deluxe\SimCity_1.dat";

            using (BinaryReader br = new BinaryReader(new FileStream(path,FileMode.Open,FileAccess.Read)))
            {
                DatHeader head = new DatHeader(br);
                DateTime create = unix.AddSeconds(head.DateCreated);
                DateTime mod = unix.AddSeconds(head.DateModified);
            }

            Console.ReadLine();


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

            Console.WriteLine("Press any key to exit");
            Console.ReadLine();
        }
    }
}
