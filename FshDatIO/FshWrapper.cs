using System;
using System.Collections.Generic;
using System.Text;
using FSHLib;
using System.IO;
//using SynapticEffect.SimCity;

namespace FshDatIO
{
    public class FshWrapper
    {
        private FSHImage image = null;
        private bool loaded = false;
        private bool compressed = false;
        private int fileindex = -1;
        private bool usefshwrite = false;


        public FshWrapper()
        {
            image = null;
            loaded = false;
            compressed = false;
        }
        /// <summary>
        /// Initilizes a new instance of the FshWrapper class with the specified FSHImage
        /// </summary>
        /// <param name="fsh">The source image to use</param>
        public FshWrapper(FSHImage fsh)
        {
            image = fsh;
            compressed = fsh.IsCompressed;
            loaded = true;
        }
        
        public void Load(Stream input)
        {
            if (input == null)
                throw new ArgumentNullException("input", "input is null.");

            image = new FSHImage(input);
            image.IsCompressed = compressed;
            this.loaded = true;
        }

        public int Save(Stream output)
        {
            if (output == null)
                throw new ArgumentNullException("output", "output is null.");

            if (image != null && image.RawData != null && image.RawData.Length > 0)
            {
                int prevpos = (int)output.Position;

                int datalen = image.RawData.Length;
               
                if (usefshwrite && IsDXTFsh(image))
                {
                    Fshwrite fw = new Fshwrite();
                    fw.Compress = image.IsCompressed;
                    foreach (BitmapItem bi in image.Bitmaps)
                    {
                        if ((bi.Bitmap != null && bi.Alpha != null))
                        {
                            fw.bmp.Add(bi.Bitmap);
                            fw.alpha.Add(bi.Alpha);
                            fw.dir.Add(bi.DirName);
                            fw.code.Add((int)bi.BmpType);
                        }
                    }
                    fw.WriteFsh(output);

                    if (image.IsCompressed && !fw.Compress)  
                    {
                        image.IsCompressed = false; // compression failed so set image.IsCompressed to false 
                    }
                }
                else
                {
                    using (MemoryStream rawstream = new MemoryStream(image.RawData)) // bypass FSHLib because it does not seem to save some images correctly
                    {
                        if (image.IsCompressed)
                        {
                            byte[] compbuf = QfsComp.Comp(rawstream);

                            if ((compbuf != null) && (compbuf.Length < image.RawData.Length)) // is compbuf not null and is its length less than the uncompressed data length
                            {
                                datalen = compbuf.Length;

                                output.Write(compbuf, 0, compbuf.Length);
                            }
                            else
                            {
                                image.IsCompressed = false; 
                                rawstream.WriteTo(output); // write the uncompressed data to the stream if the data did not compress
                            }
                        }
                        else
                        {
                            rawstream.WriteTo(output);
                        }
                    }
                }

                int len = ((int)output.Position - prevpos);

#if DEBUG
                System.Diagnostics.Debug.WriteLine(string.Format("datalen = {0} len = {1}", datalen.ToString(), len.ToString())); 
#endif

                return len;
            }
            return -1;
        }

        /// <summary>
        /// Test if the fsh only contains DXT1 or DXT3 items
        /// </summary>
        /// <param name="image">The image to test</param>
        /// <returns>True if successful otherwise false</returns>
        private static bool IsDXTFsh(FSHImage image)
        {
            bool result = true;
            foreach (BitmapItem bi in image.Bitmaps)
            {
                if (bi.BmpType != FSHBmpType.DXT3 && bi.BmpType != FSHBmpType.DXT1)
                {
                    result = false;
                }
            }
            return result;
        }

        public FSHImage Image
        {
            get 
            {
                return image;
            }
        }

        

        public bool Compressed
        {
            get
            {
                if (image != null) 
                {
                    compressed = image.IsCompressed;
                }
                return compressed;
            }
            set
            {
                compressed = value;
                if (image != null)
                {
                    image.IsCompressed = compressed;
                }
            }
        }
        public bool Loaded
        {
            get
            {
                return loaded;
            }
        }

        public int FileIndex // used for the FromDatIndex function
        {
            get
            {
                return fileindex;
            }
            set
            {
                fileindex = value;
            }
        }

        public bool UseFshWrite
        {
            get
            {
                return usefshwrite;
            }
            set
            {
                usefshwrite = value;
            }
        }

      
    }
}
