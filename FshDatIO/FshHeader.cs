namespace FshDatIO
{
    /// <summary>
    /// The structure that represents the FSH file header. 
    /// </summary>
    public struct FSHHeader
    {
        /// <summary>
        /// The FSH file identifier - 'SHPI'
        /// </summary>
        public byte[] SHPI;
        /// <summary>
        /// The total size it the file in bytes including this header
        /// </summary>
        public int size;
        /// <summary>
        /// The number of images within the file (may include a global palette).
        /// </summary>
        public int imageCount;
        /// <summary>
        /// The image family identifier.
        /// </summary>
        public byte[] dirID;
    }
}