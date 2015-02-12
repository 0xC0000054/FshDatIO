using System;
using System.IO;
using System.Text;

namespace FshDatIO
{
	/// <summary>
	/// The class that represents the FSH directory header.
	/// </summary>
	public sealed class FSHDirEntry
	{
		private byte[] name;
		private int offset;

		internal const int SizeOf = 8;

		/// <summary>
		/// The name of the directory.
		/// </summary>
		public string Name
		{
			get
			{
				return Encoding.ASCII.GetString(this.name, 0, 4);
			}
		}
		
		/// <summary>
		/// The offset to the <see cref="EntryHeader"/>.
		/// </summary>
		public int Offset
		{
			get
			{
				return this.offset;
			}
			internal set
			{
				this.offset = value;
			}
		}

		internal FSHDirEntry(byte[] rawData, int startOffset)
		{
			if (rawData == null)
			{
				throw new ArgumentNullException("rawData");
			}

			if (startOffset < 0 || startOffset > (rawData.Length - FSHDirEntry.SizeOf))
			{
				throw new ArgumentOutOfRangeException("startOffset");
			}

			this.name = new byte[4];
			Array.Copy(rawData, startOffset, this.name, 0, 4);
			this.offset = LittleEndianBitConverter.ToInt32(rawData, startOffset + 4);
		}

		internal FSHDirEntry(string name)
		{
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}

			if (name.Length == 4)
			{
				this.name = Encoding.ASCII.GetBytes(name);
			}
			else
			{
				this.name = Encoding.ASCII.GetBytes("0000");
			}
			this.offset = 0;
		}

		internal void Save(Stream stream)
		{
			stream.Write(this.name, 0, 4);
			stream.WriteInt32(this.offset);
		}
	}
}