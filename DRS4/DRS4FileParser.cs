using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NovoParser.DRS4
{
	public class DRS4FileParser
	{
		public enum DRS4FileFlag { File, Time, Event, Channel };

		private static readonly Regex fileRegex = new Regex("^DRS\\d{1}$");
		private static readonly Regex channelRegex = new Regex("^C\\d{3}$");
		private static readonly Regex eventHeaderRegex = new Regex("^EHDR$");
		private static readonly Regex timeHeaderRegex = new Regex("^TIME$");

		private FileStream file;

		private DRS4FileParser() : this(null) { }
		private DRS4FileParser(FileStream stream)
		{
			file = stream;
		}
		static DRS4FileParser() { }
		public static DRS4FileParser NewParser(FileStream stream)
		{
			return new DRS4FileParser(stream);
		}

#warning Refactor: Implement more extensive validation based on header offsets.
		/// <summary>
		/// Method to validate DRS4 binary data file.
		/// </summary>
		/// <returns>True if valid, else false.</returns>
		public bool Validate()
		{
			byte[] file_word = new byte[4];

			long temp_pos = file.Position;
			file.Position = 0;
			file.Read(file_word, 0, 4);
			file.Position = temp_pos;

			Convert.ToChar(file_word[0]);
			string str_word = LineString(file_word);

			return fileRegex.IsMatch(str_word);
		}

		public async Task<DRS4FileData> ParseAsync()
		{
			DRS4FileData Data = new DRS4FileData();
			{
				long temp_pos = file.Position;
				file.Position = 3;
				byte versByte = (byte)file.ReadByte();
				file.Position = temp_pos;
				Data.Version = Byte.Parse(Convert.ToChar(versByte).ToString());
			}

			SortedDictionary<long, DRS4FileFlag> DRS4FileFlagDict = BuildDictionary();

			// Preparation and async excecution of parser function for DRS4Time object. 
			Task<DRS4Time> tskTimeData;
			{
				long read_start_pos = 0;
				long read_end_pos = 0;
				foreach (var item in DRS4FileFlagDict)
				{
					if (item.Value == DRS4FileFlag.Time)
					{
						read_start_pos = item.Key + 4;
					}
					else if (item.Value == DRS4FileFlag.Event)
					{
						read_end_pos = item.Key;
						break;
					}
				}

				int read_length = (int)(read_end_pos - read_start_pos);

				byte[] timeData = new byte[read_length];
				long temp_pos = file.Position;
				file.Position = read_start_pos;
				file.Read(timeData, 0, read_length);
				file.Position = temp_pos;

				tskTimeData = Task.Run(() => ParseTime(timeData));
			}

			// Preparation and async excecution of parser functions for DRS4Event objects. 
			List<Task<DRS4Event>> tskEventData = new List<Task<DRS4Event>>();
			{
				List<long> eventPos = new List<long>();
				foreach (var item in DRS4FileFlagDict)
				{
					if (item.Value == DRS4FileFlag.Event)
					{
						eventPos.Add(item.Key);
					}
				}
				long temp_pos = file.Position;
				for (int i = 0; i < eventPos.Count - 1; i++)
				{
					file.Position = eventPos[i] + 4;
					int read_length = (int)(eventPos[i + 1] - (eventPos[i] + 4));
					byte[] eventData = new byte[read_length];
					file.Read(eventData, 0, read_length);
					tskEventData.Add(Task.Run(() => ParseEvent(eventData)));
				}
				file.Position = temp_pos;
			}

			// Await result of DRS4Time object parsing
			Data.Time = await tskTimeData;

			// Await results of DRS4Event object parsing
			List<DRS4Event> events = new List<DRS4Event>();
			for (int i = 0; i < tskEventData.Count; i++)
			{
				events.Add(await tskEventData[i]);
			}

			Data.Events = events;
			return Data;
		}

		/// <summary>
		/// Parses binary data from a byte array into a DRS4Time object, based on then DRS4 binary data format.
		/// For use data after "TIME" headers.
		/// </summary>
		/// <param name="data">Array of bytes between two time headers, not including the headers</param>
		/// <returns>DRS4 binary data as a DRS4Time</returns>
		private DRS4Time ParseTime(byte[] data)
		{
			DRS4Time time = new DRS4Time()
			{
				BoardNumber = BitConverter.ToUInt16(data, 2),
				TimeData = new List<DRS4TimeData>()
			};

			for (int i = 4; i < data.Length; i += 1025 * 4)
			{
				int ii = i;
				DRS4TimeData timeData = new DRS4TimeData();
				{
					byte[] byte_channel_num = { data[ii + 1], data[ii + 2], data[ii + 3] };
					string channel = $"{Convert.ToChar(byte_channel_num[0])}{Convert.ToChar(byte_channel_num[1])}{Convert.ToChar(byte_channel_num[2])}";
					timeData.ChannelNumber = Byte.Parse(channel);
				}

				float[] timeFloats = new float[1024];
				{
					ii += 4;
					for (int j = 0; j < 1024; j++)
					{
						timeFloats[j] = BitConverter.ToSingle(data, ii);
						ii += 4;
					}
					timeData.Data = timeFloats;
				}

				time.TimeData.Add(timeData);
			}

			return time;
		}

		/// <summary>
		/// Parses binary data from a byte array into a DRS4Event object, based on then DRS4 binary data format.
		/// For use data after "EVNT" headers.
		/// </summary>
		/// <param name="data">Array of bytes between two event headers, not including the headers</param>
		/// <returns>DRS4 binary data as a DRS4Event</returns>
		private DRS4Event ParseEvent(byte[] data) 
		{
			DRS4Event @event = new DRS4Event()
			{
				EventSerialNumber = BitConverter.ToUInt32(data, 0),
				EventTime = new DateTime(
					year: BitConverter.ToUInt16(data, 4),
					month: BitConverter.ToUInt16(data, 6),
					day: BitConverter.ToUInt16(data, 8),
					hour: BitConverter.ToUInt16(data, 10),
					minute: BitConverter.ToUInt16(data, 12),
					second: BitConverter.ToUInt16(data, 14),
					millisecond: BitConverter.ToUInt16(data, 16)
				),
				RangeCenter = BitConverter.ToInt16(data, 18),
				BoardNumber = BitConverter.ToUInt16(data, 22),
				TriggerCell = BitConverter.ToUInt16(data, 26),
				EventData = new List<DRS4EventData>()
			};

			for (int i = 28; i < data.Length; i += (512 + 2) * 4)
			{
				int ii = i;
				DRS4EventData eventData = new DRS4EventData();
				{
					byte[] byte_channel_num = { data[ii + 1], data[ii + 2], data[ii + 3] };
					string channel = $"{Convert.ToChar(byte_channel_num[0])}{Convert.ToChar(byte_channel_num[1])}{Convert.ToChar(byte_channel_num[2])}";
					eventData.ChannelNumber = Byte.Parse(channel);
				}

				ushort[] voltageShorts = new ushort[1024];
				{
					ii += 4;
					eventData.Scaler = BitConverter.ToInt32(data, ii);
					for (int j = 0; j < 1024; j++)
					{
						voltageShorts[j] = BitConverter.ToUInt16(data, ii);
						ii += 2;
					}
					eventData.Voltage = voltageShorts;
				}

				@event.EventData.Add(eventData);
			}

			return @event;
		}

		/// <summary>
		/// Makes a dictionary of headers and their position in the stream/file.
		/// </summary>
		/// <returns>Sorted dictionary with header start position as "key" and DRS4FileFlag enum value as "value"</returns>
		private SortedDictionary<long, DRS4FileFlag> BuildDictionary()
		{
			SortedDictionary<long, DRS4FileFlag> FileFlags = new SortedDictionary<long, DRS4FileFlag>();

			FileFlags.Add(0, DRS4FileFlag.File);

			long temp_pos = file.Position;
			file.Position = 4;
			while (file.Position < file.Length)
			{
				byte[] file_word = new byte[4];

				file.Read(file_word, 0, 4);
				string str_word = LineString(file_word);

				if (timeHeaderRegex.IsMatch(str_word))
				{
					FileFlags.Add(file.Position - 4, DRS4FileFlag.Time);
				}
				else if (eventHeaderRegex.IsMatch(str_word))
				{
					FileFlags.Add(file.Position - 4, DRS4FileFlag.Event);
				}
				else if (channelRegex.IsMatch(str_word))
				{
					FileFlags.Add(file.Position - 4, DRS4FileFlag.Channel);
				}
			}

			file.Position = temp_pos;

			return FileFlags;
		}

		/// <summary>
		/// Method to parse 4 bytes into a string.
		/// Used to find headers in binary data file
		/// </summary>
		/// <param name="line">Byte array of length 4, form binary data file</param>
		/// <returns>Parsed string</returns>
		private string LineString(byte[] line)
		{
			return $"{Convert.ToChar(line[0])}{Convert.ToChar(line[1])}{Convert.ToChar(line[2])}{Convert.ToChar(line[3])}";
		}
	}
}
