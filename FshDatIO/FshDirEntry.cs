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

		internal FSHDirEntry(BinaryReader reader)
		{
			if (reader == null)
			{
				throw new ArgumentNullException("reader");
			}

			this.name = reader.ReadBytes(4);
			this.offset = reader.ReadInt32();
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