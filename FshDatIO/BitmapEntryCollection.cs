using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FshDatIO
{
    /// <summary>
    /// The collection of BitmapEntry items within the DatFile
    /// </summary>
    public sealed class BitmapEntryCollection : Collection<BitmapEntry>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BitmapEntryCollection"/> class.
        /// </summary>
        public BitmapEntryCollection() : base()
        {
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="BitmapEntryCollection"/> class.
        /// </summary>
        /// <param name="capacity">The capacity.</param>
        public BitmapEntryCollection(int capacity) : base(new List<BitmapEntry>(capacity))
        {
        }
    }
}
