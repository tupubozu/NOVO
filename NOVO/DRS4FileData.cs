using System;
using System.Collections.Generic;

namespace NOVO
{
	public class DRS4FileData
	{
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
		public short BoardNumber;
		public List<DRS4TimeData> TimeData;
	}
	public class DRS4TimeData
	{
		public byte ChannelNumber;
		public float[] Data;
	}
	public class DRS4Event
	{
		public int EventSerialNumber;

		public DateTime EventTime;

		public short Range;
		public ushort BoardNumber;
		public ushort TriggerCell;

		public List<DRS4EventData> EventData;
	}
	public class DRS4EventData
	{
		public byte ChannelNumber;
		public int Scaler;
		public ushort[] Voltage;
	}
}
