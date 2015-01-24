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
				return size;
			}
			internal set
			{
				size = value;
			}
		}

		/// <summary>
		/// The number of images within the file (may include a global palette).
		/// </summary>
		public int ImageCount
		{
			get 
			{ 
				return imageCount; 
			}
		}

		/// <summary>
		/// The image family identifier.
		/// </summary>
		public string DirectoryId
		{
			get 
			{ 
				return Encoding.ASCII.GetString(directoryId);
			}
		}
		
		internal FSHHeader(BinaryReader reader)
		{
			if (reader == null)
			{
				throw new ArgumentNullException("reader");
			}

			if (reader.ReadUInt32() != FSHSignature)
			{
				throw new FormatException(Properties.Resources.InvalidFshHeader);
			}

			this.size = reader.ReadInt32();
			this.imageCount = reader.ReadInt32();
			this.directoryId = reader.ReadBytes(4);
		}

		internal FSHHeader(int imageCount, string directoryID)
		{
			this.size = 0;
			this.imageCount = imageCount;
			this.directoryId = Encoding.ASCII.GetBytes(directoryID);
		}

		internal void Save(Stream stream)
		{
			stream.WriteUInt32(FSHSignature);
			stream.WriteInt32(this.size);
			stream.WriteInt32(this.imageCount);
			stream.Write(this.directoryId, 0, 4);
		}
	}
}