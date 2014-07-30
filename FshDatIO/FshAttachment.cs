using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FshDatIO
{
	public sealed class FshAttachment
	{
		private EntryHeader header;
		private byte[] data;
		private bool binaryData;

		public EntryHeader Header
		{
			get
			{
				return header;
			}
		}

		public byte[] GetData()
		{
			return data;
		}

		public bool BinaryData
		{
			get
			{
				return binaryData;
			}
		}

		internal FshAttachment(EntryHeader header, byte[] data, bool binaryData)
		{
			this.header = header;
			this.data = data;
			this.binaryData = binaryData;
		}
	}
}
