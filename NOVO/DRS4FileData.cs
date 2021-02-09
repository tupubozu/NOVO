using System;
using System.Collections.Generic;

namespace NOVO
{
	public class DRS4FileData
	{
		public byte Version;
		public DRS4Time Time;
		public List<DRS4Event> Events;
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
		public short BoardNumber;
		public short TriggerCell;

		public List<DRS4EventData> EventData;
	}
	public class DRS4EventData
	{
		public byte ChannelNumber;
		public int Scaler;
		public short[] Voltage;
	}

}
