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
    }
}
