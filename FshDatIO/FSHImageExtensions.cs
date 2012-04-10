using System;
using System.Reflection;
using FSHLib;

namespace FshDatIO
{
    static class FSHImageExtensions
    {
        private static FieldInfo rawDataField;
        private static FieldInfo directoryField;
        public static void SetRawData(this FSHImage fsh, byte[] data)
        {
            if (rawDataField == null)
	        {
		        Type fshImageType = typeof(FSHImage);
                rawDataField = fshImageType.GetField("rawData", BindingFlags.Instance | BindingFlags.NonPublic);
	        }
            rawDataField.SetValue(fsh, data);
        }

        public static void SetDirectories(this FSHImage fsh, FSHDirEntry[] dirs)
        {
            if (directoryField == null)
            {
                Type fshImageType = typeof(FSHImage);
                directoryField = fshImageType.GetField("directory", BindingFlags.Instance | BindingFlags.NonPublic);
            }
            directoryField.SetValue(fsh, dirs);
        }
    }
}
