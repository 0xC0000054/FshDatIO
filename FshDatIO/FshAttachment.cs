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
	/// The class that encapsulates an attachment within a FSH file.
	/// </summary>
	public sealed class FshAttachment
	{
		private EntryHeader header;
		private byte[] data;

		/// <summary>
		/// Gets the attachment header.
		/// </summary>
		/// <value>
		/// The attachment header.
		/// </value>
		public EntryHeader Header
		{
			get
			{
				return this.header;
			}
		}

		/// <summary>
		/// Gets the attachment data.
		/// </summary>
		/// <returns>The attachment data.</returns>
		public byte[] GetData()
		{
			return this.data;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="FshAttachment"/> class.
		/// </summary>
		/// <param name="header">The header.</param>
		/// <param name="data">The data.</param>
		internal FshAttachment(EntryHeader header, byte[] data)
		{
			this.header = header;
			this.data = data;
		}
	}
}
