using System;
using System.Collections.Generic;
using System.Text;
using FSHLib;
using System.IO;

namespace FshDatIO
{
    public sealed class FshWrapper : IDisposable
    {
        private FSHImageWrapper image;
        private bool loaded;
        private bool compressed;
        private int fileIndex;
        private bool useFshWrite;

        private bool disposed;

        public FshWrapper()
        {
            this.image = null;
            this.loaded = false;
            this.compressed = false;
            this.fileIndex = -1;
            this.useFshWrite = false;
            this.disposed = false;
        }
        /// <summary>
        /// Initilizes a new instance of the FshWrapper class with the specified FSHImage
        /// </summary>
        /// <param name="fsh">The source image to use</param>
        /// <exception cref="System.ArgumentNullException">The FSHImage is null.</exception>
        public FshWrapper(FSHImageWrapper fsh)
        {
            if (fsh == null)
                throw new ArgumentNullException("fsh", "fsh is null.");
            image = fsh;
            compressed = fsh.IsCompressed;
            loaded = true;
        }

        public void Load(Stream input)
        {
            if (input == null)
                throw new ArgumentNullException("output", "output is null.");

            image = new FSHImageWrapper(input);
            image.IsCompressed = compressed;
            this.loaded = true;
        }

        /// <summary>
        /// Saves the FSHImageWrapper instance to the specified output stream.
        /// </summary>
        /// <param name="output">The output stream to save to.</param>
        /// <returns>The length of the saved data.</returns>
        [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.LinkDemand, Flags = System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
        public int Save(Stream output)
        {
            if (output == null)
                throw new ArgumentNullException("output", "output is null.");

            if (image != null && image.RawData != null && image.RawData.Length > 0)
            {
                int prevpos = (int)output.Position;

                int datalen = image.RawData.Length;

                if (useFshWrite && IsDXTFsh(image))
                {
                    image.Save(output, true);
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
        private static bool IsDXTFsh(FSHImageWrapper image)
        {
            bool result = true;
            foreach (BitmapEntry item in image.Bitmaps)
            {
                if (item.BmpType != FSHBmpType.DXT3 && item.BmpType != FSHBmpType.DXT1)
                {
                    result = false;
                }
            }
            return result;
        }

        public FSHImageWrapper Image
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
                return fileIndex;
            }
            internal set
            {
                fileIndex = value;
            }
        }
        /// <summary>
        /// Use FshWrite Compression when saving the image
        /// </summary>
        public bool UseFshWrite
        {
            get
            {
                return useFshWrite;
            }
            set
            {
                useFshWrite = value;
            }
        }

        public void Dispose()
        {
            if (!disposed)
            {
                if (image != null)
                {
                    image.Dispose();
                    image = null;
                }

                this.disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }
}
