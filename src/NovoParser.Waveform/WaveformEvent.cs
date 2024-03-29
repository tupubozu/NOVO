using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using NovoParser.ParserOptions;

namespace NovoParser.Waveform
{
	/// <summary>
	/// WaveformEvent is a high level abstraction of the DRS4Event and DRS4Time objects.
	/// Its purpose is to contain sets of waveforms, with associated timestamps and voltages represented with floating point datatypes. 
	/// </summary>
	public class WaveformEvent : IComparable<WaveformEvent>, IComparer<WaveformEvent>
	{
		private static double relativeThresholdVoltage;
		private static int trimOffset;

		public DateTime EventDateTime;
		public ushort BoardNumber;
		public uint SerialNumber;
		public short RangeCenter;
		public ushort TriggerCell;


		public List<WaveformData> Channels;

		private static NumberFormatInfo numberFormat;

		static WaveformEvent()
		{
			numberFormat = new NumberFormatInfo()
			{
				CurrencyDecimalSeparator = ".",
				NumberDecimalSeparator = ".",
				PercentDecimalSeparator = "."
			};

			relativeThresholdVoltage = 495.0;
			trimOffset = 15;
		}

		public bool IsOutOfRange
		{
			get
			{
				for (int i = 0; i < Channels.Count; i++)
				{
					foreach (WaveformSample sample in Channels[i].Samples)
					{
						if (sample.VoltageComponent > RangeCenter + relativeThresholdVoltage || sample.VoltageComponent < RangeCenter - relativeThresholdVoltage)
						{
							return true;
						}
					}
				}
				return false;
			}
		}

		public void Trim(bool trimStart = true, bool trimEnd = true)
		{
			if (trimStart) TrimStart();
			if (trimEnd) TrimEnd();
		}
		public void TrimStart()
		{
			foreach (WaveformData channel in Channels)
			{
				TrimChannelStart(channel);
			}
		}
		private void TrimChannelStart(WaveformData channel)
		{
			for (int i = trimOffset; i < channel.Samples.Count; i++)
			{
				double temp_prev = 0.0;
				for (int j = i - trimOffset; j < i && j < channel.Samples.Count; j++)
				{
					temp_prev += channel.Samples[j].VoltageComponent;
				}

				double temp_next = 0.0;
				for (int j = i; j < i + trimOffset && j < channel.Samples.Count; j++)
				{
					temp_next += channel.Samples[j].VoltageComponent;
				}

				if ((Math.Abs(temp_next / trimOffset) > Math.Abs(temp_prev / trimOffset)) && (Math.Abs(channel.Samples[i].VoltageComponent) >= Options.ThresholdVoltage))
				{
					if (i - trimOffset > 0) channel.Samples.RemoveRange(0, i - trimOffset);
					return;
				}
			}
		}
		public void TrimEnd()
		{
			foreach (WaveformData channel in Channels)
			{
				TrimChannelEnd(channel);
			}
		}
		private void TrimChannelEnd(WaveformData channel)
		{
			for (int i = channel.Samples.Count - trimOffset - 1; i >= 0; i--)
			{
				double temp_prev = 0.0;
				for (int j = i + trimOffset; j > i && j >= 0; j--)
				{
					temp_prev += channel.Samples[j].VoltageComponent;
				}
				double temp_next = 0.0;
				for (int j = i; j > i - trimOffset && j >= 0; j--)
				{
					temp_next += channel.Samples[j].VoltageComponent;
				}

				if ((Math.Abs(temp_next / trimOffset) > Math.Abs(temp_prev / trimOffset)) && (Math.Abs(channel.Samples[i].VoltageComponent) >= Options.ThresholdVoltage))
				{
					channel.Samples.RemoveRange(i + trimOffset, channel.Samples.Count - (i + trimOffset) - 1);
					return;
				}
			}
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

		public string ToCSV(double sample_time)
		{
			double startTime = double.MaxValue;
			double stopTime = double.MinValue;
			foreach (WaveformData channel in Channels)
			{
				if (channel.Samples[0].TimeComponent < startTime)
					startTime = channel.Samples[0].TimeComponent;
			}
			foreach (WaveformData channel in Channels)
			{
				if (channel.Samples[channel.Samples.Count - 1].TimeComponent > stopTime)
					stopTime = channel.Samples[channel.Samples.Count - 1].TimeComponent;
			}
			return ToCSV(startTime, stopTime, sample_time);
		}

		public string ToCSV(double start_time, double stop_time, double sample_time)
		{
			int digits = (int)Math.Round(Math.Abs(Math.Log10(sample_time)) + 0.5, 0);

			string output_str = "\"Time\",";

			for (int j = 0; j < Channels.Count; j++)
			{
				output_str += $"\"Channel {Channels[j].ChannelNumber}\"";
				if (!(j >= Channels.Count)) output_str += ",";
			}
			output_str += "\n";

			for (double i = start_time; i < stop_time; i += sample_time)
			{
				i = Math.Round(i, digits);
				string temp_str = string.Format("\"{0}\",", i.ToString(numberFormat));
				for (int j = 0; j < Channels.Count; j++)
				{
					temp_str += string.Format("\"{0}\"", Channels[j].Interpolate(i).ToString(numberFormat));
					if (j < (Channels.Count - 1)) temp_str += ",";
				}
				temp_str += "\n";

				output_str += temp_str;
			}

			return output_str;
		}

		public async Task<string[]> ToCSVAsync()
		{
			SortedSet<double> times = new SortedSet<double>();

			foreach (var channel in Channels)
			{
				foreach (var sample in channel.Samples)
				{
					times.Add(sample.TimeComponent);
				}
			}

			List<Task<string>> tskOutput = new List<Task<string>>();

			tskOutput.Add(Task.Run(() =>
			{
				string output_str = "\"Time\",";
				for (int j = 0; j < Channels.Count; j++)
				{
					output_str += $"\"Channel {Channels[j].ChannelNumber}\"";
					if (j < (Channels.Count - 1)) output_str += ",";
				}
				return output_str;
			}));

			foreach (double time in times)
			{
				double alias_i = time;

				tskOutput.Add(Task.Run(() =>
				{
					string temp_str = string.Format("\"{0}\",", alias_i.ToString(numberFormat));

					for (int j = 0; j < Channels.Count; j++)
					{
						temp_str += string.Format("\"{0}\"", Channels[j].Interpolate(alias_i).ToString(numberFormat));
						if (j < (Channels.Count - 1)) temp_str += ",";
					}
					return temp_str;
				}));
			}

			string[] output = new string[tskOutput.Count];

			for (int i = 0; i < output.Length; i++)
			{
				output[i] = await tskOutput[i];
				tskOutput[i].Dispose();
			}

			return output;
		}

		public async Task<string[]> ToCSVAsync(double sample_time)
		{
			double startTime = double.MaxValue;
			double stopTime = double.MinValue;
			foreach (WaveformData channel in Channels)
			{
				if (channel.Samples[0].TimeComponent < startTime)
					startTime = channel.Samples[0].TimeComponent;
			}
			foreach (WaveformData channel in Channels)
			{
				if (channel.Samples[channel.Samples.Count - 1].TimeComponent > stopTime)
					stopTime = channel.Samples[channel.Samples.Count - 1].TimeComponent;
			}
			return await ToCSVAsync(startTime - sample_time, stopTime + sample_time, sample_time);
		}

		public async Task<string[]> ToCSVAsync(double start_time, double stop_time, double sample_time)
		{
			int digits = (int)Math.Round(Math.Abs(Math.Log10(sample_time)) + 0.5, 0);

			List<Task<string>> tskOutput = new List<Task<string>>();

			tskOutput.Add(Task.Run(() =>
		  {
			  string output_str = "\"Time\",";
			  for (int j = 0; j < Channels.Count; j++)
			  {
				  output_str += $"\"Channel {Channels[j].ChannelNumber}\"";
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
						temp_str += string.Format("\"{0}\"", Channels[j].Interpolate(alias_i).ToString(numberFormat));
						if (j < (Channels.Count - 1)) temp_str += ",";
					}
					return temp_str;
				}));
			}

			string[] output = new string[tskOutput.Count];

			for (int i = 0; i < output.Length; i++)
			{
				output[i] = await tskOutput[i];
				tskOutput[i].Dispose();
			}

			return output;
		}
	}
}
