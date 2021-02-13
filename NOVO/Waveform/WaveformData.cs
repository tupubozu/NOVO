using System;
using System.Collections.Generic;

namespace NOVO.Waveform
{
	public class WaveformData
	{
		public byte ChannelNumber;
		public List<WaveformSample> Samples;
 
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
