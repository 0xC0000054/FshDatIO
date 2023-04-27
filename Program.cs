/*
*  This file is part of FshDatIO, a library that manipulates SC4
*  DBPF files and FSH images.
*
*  Copyright (C) 2010-2017, 2023 Nicholas Hayes
*
*  This program is free software: you can redistribute it and/or modify
*  it under the terms of the GNU General Public License as published by
*  the Free Software Foundation, either version 3 of the License, or
*  (at your option) any later version.
*
*  This program is distributed in the hope that it will be useful,
*  but WITHOUT ANY WARRANTY; without even the implied warranty of
*  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
*  GNU General Public License for more details.
*
*  You should have received a copy of the GNU General Public License
*  along with this program.  If not, see <http://www.gnu.org/licenses/>.
*
*/

using System;
using System.Drawing;
using FshDatIO;

namespace readfshdat
{
    class Program
    {

        static void Main(string[] args)
        {
            if (args.Length == 1)
            {
                string dat = args[0];
                Console.WriteLine(dat);

                DatFile dl = new DatFile(dat);
                try
                {
                    int i = 0;
                    foreach (var item in dl.Indexes)
                    {
                        if (item.Type == 0x7ab50e44)
                        {
                            FshFileItem fi = dl.LoadFile(item.Group, item.Instance);
                            FSHImageWrapper image = fi.Image;

                            BitmapEntryCollection bitmapEntries = image.Bitmaps;
                            BitmapEntry bitmapEntry = image.Bitmaps[0];
                            Bitmap bitmap = bitmapEntry.Bitmap;

                            Console.WriteLine(string.Format("Fsh {0}: {1} images, First item: {2}x{3} {4}",
                                                            i++,
                                                            bitmapEntries.Count,
                                                            bitmap.Width,
                                                            bitmap.Height,
                                                            bitmapEntry.BmpType));
                        }
                    }
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
            }

            Console.WriteLine("\n Press any key to exit");
            Console.ReadLine();
        }
    }
}
