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

using System;
using System.Runtime.Serialization;

namespace FshDatIO
{
    /// <summary>
    /// The Exception thrown when a <see cref="DatIndex"/> does not exist within the <see cref="DatFile"/>
    /// </summary>
    [Serializable]
    public sealed class DatIndexException : DatFileException, ISerializable  
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DatIndexException"/> class.
        /// </summary>
        public DatIndexException() : base("A DatIndexException has occurred")
        {}

        /// <summary>
        /// Initializes a new instance of the <see cref="DatIndexException"/> class with the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        public DatIndexException(string message) : base(message)
        {
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="DatIndexException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="inner">The inner exception.</param>
        public DatIndexException(string message, Exception inner) : base(message, inner)
        {
        }

        private DatIndexException(SerializationInfo info, StreamingContext context) : base(info, context)
        { 
        }
    }
}
