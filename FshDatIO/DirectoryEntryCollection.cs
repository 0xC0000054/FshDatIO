/*
*  This file is part of FshDatIO, a library that manipulates SC4
*  DBPF files and FSH images.
*
*  Copyright (C) 2010-2017, 2023 Nicholas Hayes
*
*  This program is free software: you can redistribute it and/or modify
*  it under the terms of the GNU General Public License as published by
*  the Free Software Foundation, either version 3 of the License, or
*  (at your option) any later version.
*
*  This program is distributed in the hope that it will be useful,
*  but WITHOUT ANY WARRANTY; without even the implied warranty of
*  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
*  GNU General Public License for more details.
*
*  You should have received a copy of the GNU General Public License
*  along with this program.  If not, see <http://www.gnu.org/licenses/>.
*
*/

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FshDatIO
{
    internal sealed class DirectoryEntryCollection : Collection<DirectoryEntry>
    {
        public DirectoryEntryCollection() : this(0)
        {
        }

        public DirectoryEntryCollection(int count) : base(new List<DirectoryEntry>(count))
        {
        }

        public DirectoryEntry Find(uint type, uint group, uint instance)
        {
            IList<DirectoryEntry> items = Items;

            for (int i = 0; i < items.Count; i++)
            {
                DirectoryEntry entry = items[i];
                if (entry.Type == type && entry.Group == group && entry.Instance == instance)
                {
                    return entry;
                }
            }

            return null;
        }
    }
}
