using System;
using System.Collections.Generic;
using System.Globalization;

namespace NOVO.Waveform
{
	public class WaveformEvent : IComparable<WaveformEvent>, IComparer<WaveformEvent>
	{
		// WaveformEvent is a high level abstraction of the DRS4Event and DRS4Time objects.
		// Its purpose is to contain sets of waveforms, with associated timestamps and voltages represented with floating point datatypes.  

		public DateTime EventDateTime;
		public ushort BoardNumber;

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

		public void ShiftTime(double timeDifference)
		{
			foreach (WaveformData channel in Channels)
			{
				channel.ShiftTime(timeDifference); 
			}
		}

		public string ToCSV(double start_time, double stop_time, double sample_time)
		{
			NumberFormatInfo numberFormat = new()
			{
				CurrencyDecimalSeparator = ".",
				NumberDecimalSeparator = ".",
				PercentDecimalSeparator = "."
			};

			int digits = (int)Math.Round(Math.Abs(Math.Log10(sample_time)) + 0.5, 0);

			string output_str = "\"Time\",";

			for (int j = 0; j < Channels.Count; j++)
			{
				output_str += $"\"Channel {j}\"";
				if (!(j >= Channels.Count)) output_str += ",";
			}
			output_str += "\n";

			for (double i = start_time;  i < stop_time; i += sample_time)
			{
				output_str += string.Format("\"{0}\",", Math.Round(i, digits).ToString(numberFormat));
				for (int j = 0; j < Channels.Count; j++)
				{
					output_str += string.Format("\"{0}\"", Math.Round(Channels[j].Regression(i), digits).ToString(numberFormat));
					if (j < Channels.Count - 1) output_str += ",";
				}
				output_str += "\n";
			}
			
			return output_str;
		}
	}
}
