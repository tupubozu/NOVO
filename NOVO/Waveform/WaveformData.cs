using System;
using System.Collections.Generic;

namespace NOVO.Waveform
{
	public class WaveformData: IComparable<WaveformData>, IComparer<WaveformData>
	{
		// WaveformData is an abstraction of waveforms and their associated channels.

		public byte ChannelNumber;
		public List<WaveformSample> Samples;

		public int Compare(WaveformData x, WaveformData y)
		{
			return x.CompareTo(y);
		}

		public int CompareTo(WaveformData other)
		{
			return this.ChannelNumber.CompareTo(other.ChannelNumber);
		}

		public void ShiftTime(double timeDifference)
		{
			foreach (WaveformSample sample in Samples)
			{
				sample.ShiftTime(timeDifference);
			}
		}	
	}
	
	public class WaveformSample
	{
		// Represents an individual sample of a waveform.

		private double timeComponent;
		public double TimeComponent
		{
			get => timeComponent; 
			init => timeComponent = value; 
		}
		public double VoltageComponent{ get; init; }
		
		public WaveformSample() : this(0.0, 0.0) {}
		public WaveformSample(double time, double voltage) => (TimeComponent, VoltageComponent) = (time, voltage);

		public void ShiftTime(double timeDifference) => timeComponent += timeDifference;
	}
}
