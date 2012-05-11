using System;
using System.IO;

namespace FshDatIO
{
    /// <summary>
    /// Encapsulates a fsh format image within a <see cref="DatFile"/>.
    /// </summary>
    public sealed class FshWrapper : IDisposable
    {
        private FSHImageWrapper image;
        private bool loaded;
        private bool compressed;
        private int fileIndex;
        private bool useFshWrite;

        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="FshWrapper"/> class.
        /// </summary>
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
        /// Initilizes a new instance of the FshWrapper class with the specified FSHImageWrapper
        /// </summary>
        /// <param name="fsh">The source FSHImageWrapper to use.</param>
        /// <exception cref="System.ArgumentNullException">The FSHImageWrapper is null.</exception>
        public FshWrapper(FSHImageWrapper fsh)
        {
            if (fsh == null)
                throw new ArgumentNullException("fsh");
            image = fsh;
            compressed = fsh.IsCompressed;
            loaded = true;
        }

        /// <summary>
        /// Loads a fsh file from the specified stream.
        /// </summary>
        /// <param name="input">The input stream to load from.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when the input stream is null.</exception>
        /// <exception cref="System.FormatException">Thrown when the file is invalid.</exception>
        /// <exception cref="System.FormatException">Thrown when the header of the fsh file is invalid.</exception>
        /// <exception cref="System.FormatException">Thrown when the fsh file contains an unhandled image format.</exception>
        public void Load(Stream input)
        {
            if (input == null)
                throw new ArgumentNullException("input");

            image = new FSHImageWrapper(input);
            compressed = image.IsCompressed;
            this.loaded = true;
        }
        /// <summary>
        /// Loads a fsh file from the specified byte array.
        /// </summary>
        /// <param name="imageData">The byte array to load.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when the byte array is null.</exception>
        /// <exception cref="System.FormatException">Thrown when the file is invalid.</exception>
        /// <exception cref="System.FormatException">Thrown when the header of the fsh file is invalid.</exception>
        /// <exception cref="System.FormatException">Thrown when the fsh file contains an unhandled image format.</exception>
        public void Load(byte[] imageData)
        {
            image = new FSHImageWrapper(imageData);
            compressed = image.IsCompressed;
            this.loaded = true;
        }

        /// <summary>
        /// Saves the FSHImageWrapper instance to the specified output stream.
        /// </summary>
        /// <param name="output">The output stream to save to.</param>
        /// <returns>The length of the saved data.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown when the output stream is null.</exception>
        [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.LinkDemand, Flags = System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
        public int Save(Stream output)
        {
            if (output == null)
                throw new ArgumentNullException("output");

            byte[] rawData = image.GetRawData();

            if (image != null && rawData != null && rawData.Length > 0)
            {
                int prevpos = (int)output.Position;
                int rawDataLength = rawData.Length;

                image.Save(output, useFshWrite);

                int len = ((int)output.Position - prevpos);

#if DEBUG
                System.Diagnostics.Debug.WriteLine(string.Format("rawDataLength = {0} len = {1}", rawDataLength.ToString(), len.ToString())); 
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
            foreach (BitmapEntry item in image.Bitmaps)
            {
                if (item.BmpType != FshImageFormat.DXT3 && item.BmpType != FshImageFormat.DXT1)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Gets the image.
        /// </summary>
        public FSHImageWrapper Image
        {
            get
            {
                return image;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="FshWrapper"/> is compressed.
        /// </summary>
        /// <value>
        ///   <c>true</c> if compressed; otherwise, <c>false</c>.
        /// </value>
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
        /// <summary>
        /// Gets a value indicating whether this <see cref="FshWrapper"/> is loaded.
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
        /// Gets or sets the index of the file.
        /// </summary>
        /// <value>
        /// The index of the <see cref="FshWrapper"/> entry in the <see cref="DatFile"/>.
        /// </value>
        internal int FileIndex // used for the FromDatIndex function
        {
            get
            {
                return fileIndex;
            }
            set
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

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
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
