using System;
using System.Collections.Generic;
using System.Text;

namespace FshDatIO
{
    static class ListExtensions
    {
        public static int Find(this List<DatIndex> indexes, uint type, uint group, uint instance)
        {
            for (int i = 0; i < indexes.Count; i++)
            {
                if (indexes[i].Equals(type, group, instance))
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
            for (int i = 0; i < files.Count; i++)
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
