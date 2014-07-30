using System;
using System.IO;

namespace FshDatIO
{
    /// <summary>
    /// Encapsulates a fsh format image within a <see cref="DatFile"/>.
    /// </summary>
    public sealed class FshFileItem : IDisposable
    {
        private FSHImageWrapper image;
        private bool loaded;
        private bool compressed;
        private bool useFshWrite;

        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="FshFileItem"/> class.
        /// </summary>
        internal FshFileItem()
        {
            this.image = null;
            this.loaded = false;
            this.compressed = false;
            this.useFshWrite = false;
            this.disposed = false;
        }
        
        /// <summary>
        /// Initializes a new instance of the FshWrapper class with the specified FSHImageWrapper
        /// </summary>
        /// <param name="fsh">The source FSHImageWrapper to use.</param>
        /// <exception cref="System.ArgumentNullException">The FSHImageWrapper is null.</exception>
        public FshFileItem(FSHImageWrapper fsh) : this(fsh, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the FshWrapper class with the specified FSHImageWrapper
        /// </summary>
        /// <param name="fsh">The source FSHImageWrapper to use.</param>
        /// <exception cref="System.ArgumentNullException">The FSHImageWrapper is null.</exception>
        public FshFileItem(FSHImageWrapper fsh, bool useFshWrite)
        {
            if (fsh == null)
            {
                throw new ArgumentNullException("fsh");
            }

            this.image = fsh;
            this.compressed = fsh.IsCompressed;
            this.useFshWrite = useFshWrite;
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
        internal void Load(byte[] imageData)
        {
            if (imageData == null)
            {
                throw new ArgumentNullException("imageData");
            }

            this.image = new FSHImageWrapper(imageData);
            this.compressed = image.IsCompressed;
            this.loaded = true;
        }

        /// <summary>
        /// Saves the FSHImageWrapper instance to the specified output stream.
        /// </summary>
        /// <param name="output">The output stream to save to.</param>
        /// <returns>The length of the saved data.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown when the output stream is null.</exception>
        /// <exception cref="System.InvalidOperationException">Thrown when the image is null.</exception>
        [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.LinkDemand, Flags = System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
        internal uint Save(Stream output)
        {
            if (output == null)
            {
                throw new ArgumentNullException("output");
            }

            if (this.image == null)
            {
                throw new InvalidOperationException("image is null.");
            }

            long startOffset = output.Position;
            this.image.Save(output, useFshWrite);

            long len = (output.Position - startOffset);


            return (uint)len;
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
        /// Gets or sets a value indicating whether this <see cref="FshFileItem"/> is compressed.
        /// </summary>
        /// <value>
        ///   <c>true</c> if compressed; otherwise, <c>false</c>.
        /// </value>
        public bool Compressed
        {
            get
            {
                return compressed;
            }
            set
            {
                this.compressed = value;
                if (image != null)
                {
                    image.IsCompressed = this.compressed;
                }
            }
        }
        /// <summary>
        /// Gets a value indicating whether this <see cref="FshFileItem"/> is loaded.
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
