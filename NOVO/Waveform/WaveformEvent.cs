using System;
using System.Collections.Generic;

namespace NOVO.Waveform
{
	class WaveformEvent : IComparable<WaveformEvent>, IComparer<WaveformEvent>
	{
		// WaveformEvent is a high level abstraction of the DRS4Event and DRS4Time objects.
		// Its purpose is to contain sets of waveforms, with associated timestamps and voltages represented with floating point datatypes.  

		public DateTime EventDateTime;
		public byte BoardNumber;

		public List<WaveformData> Channels;

		public int Compare(WaveformEvent x, WaveformEvent y)
		{
			return x.CompareTo(y);
		}

		public int CompareTo(WaveformEvent other)
		{
			return this.EventDateTime.CompareTo(other.EventDateTime);
		}

		public void NormalizeTime()
		{
			double referanceTimestamp = Channels[0].Samples[0].TimeComponent;
			for (int i = 1; i < Channels.Count; i++)
			{
				double timestamp = Channels[i].Samples[0].TimeComponent;
				Channels[i].ShiftTime(referanceTimestamp - timestamp); // Possible logic error...
			}
		}
	}
}
