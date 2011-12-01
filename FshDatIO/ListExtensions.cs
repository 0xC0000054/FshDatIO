using System;
using System.Collections.Generic;
using System.Text;

namespace FshDatIO
{
    static class ListExtensions
    {
        public static int Find(this List<DatIndex> indexes, uint type, uint group, uint instance)
        {
            int count = indexes.Count;
            for (int i = 0; i < count; i++)
            {
                DatIndex index = indexes[i];
                if (index.Type == type && index.Group == group && index.Instance == instance)
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
        public static int FromDatIndex(this List<FshWrapper> files, int index)
        {
            int count = files.Count;
            for (int i = 0; i < count; i++)
            {
                if (files[i].FileIndex == index)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
