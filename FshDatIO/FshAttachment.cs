
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
