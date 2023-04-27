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
using System.IO;
using System.Text;

namespace FshDatIO
{
	/// <summary>
	/// The class that represents the FSH file header. 
	/// </summary>
	public sealed class FSHHeader
	{
		private int size;
		private int imageCount;
		private byte[] directoryId;

		/// <summary>
		/// The FSH file signature - SHPI
		/// </summary>
		private const uint FSHSignature = 0x49504853U;

		/// <summary>
		/// The total size it the file in bytes including this header
		/// </summary>
		public int Size
		{
			get
			{
				return this.size;
			}
			internal set
			{
				this.size = value;
			}
		}

		/// <summary>
		/// The number of images within the file (may include a global palette).
		/// </summary>
		public int ImageCount
		{
			get 
			{
				return this.imageCount; 
			}
		}

		/// <summary>
		/// The image family identifier.
		/// </summary>
		public string DirectoryId
		{
			get 
			{
				return Encoding.ASCII.GetString(this.directoryId);
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="FSHHeader"/> class.
		/// </summary>
		/// <param name="bytes">The bytes.</param>
		/// <exception cref="ArgumentNullException"><paramref name="bytes"/> is null.</exception>
		/// <exception cref="FormatException">The header signature is not valid.</exception>
		internal FSHHeader(byte[] bytes)
		{
			if (bytes == null)
			{
				throw new ArgumentNullException("bytes");
			}

			if (LittleEndianBitConverter.ToUInt32(bytes, 0) != FSHSignature)
			{
				throw new FormatException(Properties.Resources.InvalidFshHeader);
			}

			this.size = LittleEndianBitConverter.ToInt32(bytes, 4);
			this.imageCount = LittleEndianBitConverter.ToInt32(bytes, 8);
			this.directoryId = new byte[4];
			Array.Copy(bytes, 12, this.directoryId, 0, 4);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="FSHHeader"/> class.
		/// </summary>
		/// <param name="imageCount">The image count.</param>
		/// <param name="directoryID">The directory identifier.</param>
		internal FSHHeader(int imageCount, string directoryID)
		{
			this.size = 0;
			this.imageCount = imageCount;
			this.directoryId = Encoding.ASCII.GetBytes(directoryID);
		}

		/// <summary>
		/// Saves the <see cref="FSHHeader"/> to the specified stream.
		/// </summary>
		/// <param name="stream">The <see cref="Stream"/> where the header will be saved..</param>
		/// <exception cref="ArgumentNullException"><paramref name="stream"/> is null.</exception>
		internal void Save(Stream stream)
		{
			if (stream == null)
			{
				throw new ArgumentNullException("stream");
			}

			stream.WriteUInt32(FSHSignature);
			stream.WriteInt32(this.size);
			stream.WriteInt32(this.imageCount);
			stream.Write(this.directoryId, 0, 4);
		}
	}
}