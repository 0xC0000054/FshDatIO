namespace FshDatIO
{
    public struct FSHHeader
    {
        public byte[] SHPI;
        public int size;
        public int imageCount;
        public byte[] dirID;
    }
}