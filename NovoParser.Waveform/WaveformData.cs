using System;
using System.Collections.Generic;

namespace NovoParser.Waveform
{

    /// <summary>
    /// WaveformData is an abstraction of waveforms and their associated channels.
    /// </summary>
    public class WaveformData : IComparable<WaveformData>, IComparer<WaveformData>
	{
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

		public double Interpolate(double time)
		{
			WaveformSample sample1 = new WaveformSample(), sample2 = new WaveformSample();
			for (int i = 0; i < Samples.Count - 1; i++)
			{
				if (time >= Samples[i].TimeComponent && time <= Samples[i + 1].TimeComponent)
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
}
