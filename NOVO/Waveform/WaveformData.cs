using System;
using System.Collections.Generic;

namespace NOVO.Waveform
{
	public class WaveformData : IComparable<WaveformData>, IComparer<WaveformData>
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

		public double Regression(double time)
		{
			WaveformSample sample1 = new(), sample2 = new();
			for (int i = 0; i < Samples.Count - 1; i++)
			{
				if (time >= Samples[i].TimeComponent && time < Samples[i + 1].TimeComponent)
				{
					sample1 = Samples[i];
					sample2 = Samples[i + 1];
				}
			}

			double a = (sample2.VoltageComponent - sample1.VoltageComponent) / (sample2.TimeComponent - sample1.TimeComponent);
			if (double.IsNaN(a)) a = 0;

			return a * (time - sample1.TimeComponent) + sample1.VoltageComponent;
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
		public double VoltageComponent { get; init; }

		public WaveformSample() : this(0.0, 0.0) { }
		public WaveformSample(double time, double voltage) => (TimeComponent, VoltageComponent) = (time, voltage);

		public void ShiftTime(double timeDifference) => timeComponent += timeDifference;
	}
}
