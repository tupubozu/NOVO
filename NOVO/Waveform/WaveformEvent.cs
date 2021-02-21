using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace NOVO.Waveform
{
	public class WaveformEvent : IComparable<WaveformEvent>, IComparer<WaveformEvent>
	{
		// WaveformEvent is a high level abstraction of the DRS4Event and DRS4Time objects.
		// Its purpose is to contain sets of waveforms, with associated timestamps and voltages represented with floating point datatypes.  

		public DateTime EventDateTime;
		public ushort BoardNumber;
		public uint SerialNumber;

		public List<WaveformData> Channels;

		private static NumberFormatInfo numberFormat;

		static WaveformEvent()
		{
			numberFormat = new()
			{
				CurrencyDecimalSeparator = ".",
				NumberDecimalSeparator = ".",
				PercentDecimalSeparator = "."
			};
		}

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
			int digits = (int)Math.Round(Math.Abs(Math.Log10(sample_time)) + 0.5, 0);

			string output_str = "\"Time\",";

			for (int j = 0; j < Channels.Count; j++)
			{
				output_str += $"\"Channel {j + 1}\"";
				if (!(j >= Channels.Count)) output_str += ",";
			}
			output_str += "\n";

			for (double i = start_time;  i < stop_time; i += sample_time)
			{
				i = Math.Round(i, digits);
				string temp_str = string.Format("\"{0}\",", i.ToString(numberFormat));
				for (int j = 0; j < Channels.Count; j++)
				{
					temp_str += string.Format("\"{0}\"", Channels[j].Regression(i).ToString(numberFormat));
					if (j < (Channels.Count - 1)) temp_str += ",";
				}
				temp_str += "\n";

				output_str += temp_str;
			}
			
			return output_str;
		}

		public async Task<string[]> ToCSVAsync(double start_time, double stop_time, double sample_time)
		{
			int digits = (int)Math.Round(Math.Abs(Math.Log10(sample_time)) + 0.5, 0);

			List<Task<string>> tskOutput = new();

			tskOutput.Add( Task.Run( () =>
			{
				string output_str = "\"Time\",";
				for (int j = 0; j < Channels.Count; j++)
				{
					output_str += $"\"Channel {j + 1}\"";
					if (j < (Channels.Count - 1)) output_str += ",";
				}
				return output_str;
			}));

			for (double i = start_time; i < stop_time; i += sample_time)
			{
				i = Math.Round(i, digits);

				double alias_i = i;

				tskOutput.Add(Task.Run(() =>
				{
					string temp_str = string.Format("\"{0}\",", alias_i.ToString(numberFormat));

					for (int j = 0; j < Channels.Count; j++)
					{
						temp_str += string.Format("\"{0}\"", Channels[j].Regression(alias_i).ToString(numberFormat));
						if (j < (Channels.Count - 1)) temp_str += ",";
					}
					return temp_str;
				}));
			}

			string[] output = new string[tskOutput.Count];

			for (int i = 0; i < output.Length; i++)
			{
				output[i] = await tskOutput[i];
			}
			
			return output;
		}
	}
}
