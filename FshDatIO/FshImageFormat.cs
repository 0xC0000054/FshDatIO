namespace FshDatIO
{
    /// <summary>
    /// The image format of a <see cref="BitmapEntry"/>.
    /// </summary>
    public enum FshImageFormat : byte
    {
        ThirtyTwoBit = 0x7d,
        TwentyFourBit = 0x7f,
        DXT1 = 0x60,
        DXT3 = 0x61
    }
}