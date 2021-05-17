using NOVO.Waveform;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NOVO.DRS4File
{
	/// <summary>
	/// DRS4FileData is a 1 to 1 representation of the binary file made by a DRS4 board.
	/// </summary>
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

		public List<WaveformEvent> ToWaveformEvents()
		{
			List<WaveformEvent> output = new();

			foreach (DRS4Event event_item in this.Events)
			{
				if (event_item.BoardNumber == this.Time.BoardNumber)
					output.Add(ToWaveformEvent(event_item, this.Time));
			}

			foreach (WaveformEvent item in output)
			{
				item.NormalizeTime();
				item.Trim();
				if (item.IsOutOfRange)
					output.Remove(item);
			}

			return output;
		}

		public async Task<List<WaveformEvent>> ToWaveformEventsAsync()
		{
			List<Task<WaveformEvent>> tskOutput = new();
			foreach (DRS4Event event_item in this.Events)
			{
				if (event_item.BoardNumber == this.Time.BoardNumber)
					tskOutput.Add(Task.Run(() => ToWaveformEvent(event_item, this.Time)));
			}

			List<WaveformEvent> output = new();
			foreach (Task<WaveformEvent> worker in tskOutput)
			{
				output.Add(await worker);
				worker.Dispose();
			}

			List<WaveformEvent> removeableEvents = new();
			foreach (WaveformEvent item in output)
			{
				item.NormalizeTime();
				if (ParserOptions.Trim) item.Trim();
				if (item.IsOutOfRange && ParserOptions.Exclude)
					removeableEvents.Add(item);
			}

			foreach (WaveformEvent item in removeableEvents)
			{
				output.Remove(item);
			}
			removeableEvents.Clear();

			return output;
		}

		private WaveformEvent ToWaveformEvent(DRS4Event e, DRS4Time t)
		{
			WaveformEvent waveformEvent = new()
			{
				EventDateTime = e.EventTime,
				BoardNumber = t.BoardNumber,
				SerialNumber = e.EventSerialNumber,
				RangeCenter = e.RangeCenter,
				TriggerCell = e.TriggerCell,
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

#warning Possible logic error
					/*	Possible logic error. 
					 *	"DRS4 Evaluation BoardUser’s ManualBoard Revision 5.1" specifies summation from j = 0 to j = i - 1.
					 *	
					 *	In the event of i = 0, how does one sum the expession "dt_ch[(j+tcell)%1024]" for a 'j' starting at 0, and ending at -1?
					 *	The following code: 
					 *	
					 *		for (int i = 0; i < 1024; i++) 
					 *			for (int j = 0; j < i - 1; j++) 
					 *				t_ch[i] += dt_ch[(j + tcell) % 1024]; 
					 *	
					 *	would represent the summation algoritm. (With "t_ch" and "dt_ch" both being float arrays (FP32) of size 1024.)
					 *	
					 *	Am I to understand that I am to sum in reverse?? How does that solve anything?
					 *	Should 'j' have started at -1 and ended at 'i' instead???
					 */

					for (int j = 0; j <= i; j++)
					{
						timeComponent += temp_time_data.Data[(j + e.TriggerCell) % temp_time_data.Data.Length];
					}

					temp.Samples.Add(new(timeComponent, ((1000.0 * ed.Voltage[i]) / ushort.MaxValue) - 500 + e.RangeCenter));
				}
				waveformEvent.Channels.Add(temp);
			}

			return waveformEvent;
		}
	}

	/// <summary>
	/// DRS4Time is a representation of the data found in/after the "TIME"-header inside a DRS4 binary file.
	/// </summary>
	public class DRS4Time
	{
		public ushort BoardNumber;
		public List<DRS4TimeData> TimeData;
	}

	/// <summary>
	/// DRS4TimeData a representation of the channel spesific information found after the "TIME"-header inside a DRS4 binary file.
	/// </summary>
	public class DRS4TimeData
	{
		public byte ChannelNumber;
		public float[] Data;
	}

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

	/// <summary>
	/// DRS4EventData a representation of the channel spesific information found after the "EHDR"-header inside a DRS4 binary file.
	/// </summary>
	public class DRS4EventData
	{
		public byte ChannelNumber;
		public int Scaler;
		public ushort[] Voltage;
	}
}
