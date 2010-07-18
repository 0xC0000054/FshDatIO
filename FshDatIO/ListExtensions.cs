using System;
using System.Collections.Generic;
using System.Text;

namespace FshDatIO
{
    static class ListExtensions
    {
        public static int Find(this List<DirectoryEntry> dir, uint Type, uint Group, uint Instance)
        {
            for (int i = 0; i < dir.Count; i++)
            {
                DirectoryEntry item = dir[i];
                if ((item.Type == Type && item.Group == Group && item.Instance == Instance))
                {
                    return i;
                }
            }

            return -1;
        }

        public static int Find(this List<DatIndex> indexes, uint Type, uint Group, uint Instance)
        {
            for (int i = 0; i < indexes.Count; i++)
            {
                DatIndex item = indexes[i];
                if ((item.Type == Type && item.Group == Group && item.Instance == Instance))
                {
                    return i;
                }
            }

            return -1;
        }
        /// <summary>
        /// Finds the FshWrapper for the specified DatIndex index
        /// </summary>
        /// <param name="index">The index number to find</param>
        /// <returns>The FshWrapper at the specified index or null</returns>
        public static FshWrapper FromDatIndex(this List<FshWrapper> files, int index)
        {
            for (int i = 0; i < files.Count; i++)
            {
                FshWrapper file = files[i];
                if (file.FileIndex == index)
                {
                    return file;
                }
            }
            return null;
        }
    }
}
