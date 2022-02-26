using System;
using System.Collections.Generic;

namespace NovoParser.DRS4
{
    /// <summary>
    /// DRS4Event is a representation of the data found in/after an "EHDR"-header inside a DRS4 binary file.
    /// </summary>
    public class DRS4Event
	{
		public uint EventSerialNumber;

		public DateTime EventTime;

		public short RangeCenter;
		public ushort BoardNumber;
		public ushort TriggerCell;

		public List<DRS4EventData> EventData;
	}
}
