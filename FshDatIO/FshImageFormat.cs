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