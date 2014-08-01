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
		
		internal FshAttachment(EntryHeader header, byte[] data)
		{
			this.header = header;
			this.data = data;
		}
	}
}
