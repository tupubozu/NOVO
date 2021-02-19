using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NOVO.Waveform;

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

		public List<WaveformEvent> ToWaveformEvents()
		{
			List<WaveformEvent> output = new();

			foreach (DRS4Event event_item in this.Events)
			{
				if (event_item.BoardNumber == this.Time.BoardNumber)
					output.Add(ToWaveformEvent(event_item, this.Time));
			}

			return output;
		}

		public async Task<List<WaveformEvent>> ToWaveformEventsAsync()
		{
			List<Task<WaveformEvent>> tskOutput = new();
			foreach (DRS4Event event_item in this.Events)
			{
				if (event_item.BoardNumber == this.Time.BoardNumber)
					tskOutput.Add(Task.Run( () => ToWaveformEvent(event_item, this.Time)));
			}

			List<WaveformEvent> output = new();
			foreach (Task<WaveformEvent> worker in tskOutput)
			{
				output.Add(await worker);
			}

			return output;
		}

		private WaveformEvent ToWaveformEvent(DRS4Event e, DRS4Time t)
		{
			WaveformEvent waveformEvent = new()
			{
				EventDateTime = e.EventTime,
				BoardNumber = t.BoardNumber,
				Channels = new()
			};

			foreach (DRS4EventData ed in e.EventData)
			{
				WaveformData temp = new()
				{
					ChannelNumber = ed.ChannelNumber,
					Samples = new()
				};

				for (int i = 0; i < ed.Voltage.Length; i++)
				{
					double timeComponent = 0;
					
					DRS4TimeData temp_time_data = null;
					foreach (DRS4TimeData data_item in t.TimeData)
					{
						if (data_item.ChannelNumber == temp.ChannelNumber) temp_time_data = data_item;
					}

					for (int j = 0; j < temp_time_data.Data.Length; j++)
					{
						timeComponent += temp_time_data.Data[((j + e.TriggerCell) % temp_time_data.Data.Length)];
					}

					temp.Samples.Add(new(timeComponent, (double)ed.Voltage[i]/ushort.MaxValue - 500 - e.Range )); // Possible logic error...
				}
				waveformEvent.Channels.Add(temp);
			}

			return waveformEvent;
		}
	}
	public class DRS4Time
	{
		// DRS4Time is a representation of the data found in/after the "TIME"-header inside a DRS4 binary file.

		public ushort BoardNumber;
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
