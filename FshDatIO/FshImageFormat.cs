namespace FshDatIO
{
    /// <summary>
    /// The image format of a <see cref="BitmapEntry"/>.
    /// </summary>
    public enum FshImageFormat : byte
    {

        /// <summary>
        /// 32-bit ARGB (8:8:8:8)
        /// </summary>
        ThirtyTwoBit = 0x7d, 
        /// <summary>
        /// 24-bit RGB (0:8:8:8)
        /// </summary>
        TwentyFourBit = 0x7f,
        /// <summary>
        /// DXT1 4x4 block compression  
        /// </summary>
        DXT1 = 0x60,
        /// <summary>
        /// DXT3 4x4 block compression  
        /// </summary>
        DXT3 = 0x61
    }
}