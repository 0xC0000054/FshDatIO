namespace FshDatIO
{
    /// <summary>
    /// The structure that represents the FSH directory header.
    /// </summary>
    public struct FSHDirEntry
    {
        /// <summary>
        /// The name of the directory.
        /// </summary>
        public byte[] name;
        /// <summary>
        /// The offset to the <see cref="EntryHeader"/>.
        /// </summary>
        public int offset;
    }
}