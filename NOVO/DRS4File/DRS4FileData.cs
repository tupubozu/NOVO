using System;
using System.Collections.Generic;

namespace NOVO.DRS4File
{
	public class DRS4FileData
	{
		// DRS4FileData is a 1 to 1 representation of the binary file made by a DRS4 board.
		 
		public byte Version;
		public DRS4Time Time;
		public List<DRS4Event> Events;

		public override string ToString()
		{
			string output = string.Join('\n', 
				base.ToString(),
				string.Format("\tFile version: {0}", this.Version),
				string.Format("\tBoard number: {0}", this.Time.BoardNumber),
				string.Format("\tNumber of events: {0}", this.Events.Count)
				);
			return output; 
		}
	}
	public class DRS4Time
	{
		// DRS4Time is a representation of the data found in/after the "TIME"-header inside a DRS4 binary file.

		public short BoardNumber;
		public List<DRS4TimeData> TimeData;
	}
	public class DRS4TimeData
	{
		// DRS4TimeData a representation of the channel spesific information found after the "TIME"-header inside a DRS4 binary file.
		
		public byte ChannelNumber;
		public float[] Data;
	}
	public class DRS4Event
	{
		// DRS4Event is a representation of the data found in/after an "EHDR"-header inside a DRS4 binary file.

		public int EventSerialNumber;

		public DateTime EventTime;

		public short Range;
		public ushort BoardNumber;
		public ushort TriggerCell;

		public List<DRS4EventData> EventData;
	}
	public class DRS4EventData
	{
		// DRS4EventData a representation of the channel spesific information found after the "EHDR"-header inside a DRS4 binary file.

		public byte ChannelNumber;
		public int Scaler;
		public ushort[] Voltage;
	}
}
