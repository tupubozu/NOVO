using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NOVO.DRS4File
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

		private DRS4Time ParseTime(byte[] data)
		{
			DRS4Time time = new()
			{
				BoardNumber = BitConverter.ToInt16(data, 2),
				TimeData = new()
			};

			for (int i = 4; i < data.Length; i += 1025 * 4)
			{
				int ii = i;
				DRS4TimeData timeData = new();
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

		private DRS4Event ParseEvent(byte[] data) // Argument data is array of bytes between two event headers, not including the headers
		{
			DRS4Event @event = new()
			{
				EventSerialNumber = BitConverter.ToInt32(data, 0),
				EventTime = new(
					year: BitConverter.ToUInt16(data, 4),
					month: BitConverter.ToUInt16(data, 6),
					day: BitConverter.ToUInt16(data, 8),
					hour: BitConverter.ToUInt16(data, 10),
					minute: BitConverter.ToUInt16(data, 12),
					second: BitConverter.ToUInt16(data, 14),
					millisecond: BitConverter.ToUInt16(data, 16)
				),
				Range = BitConverter.ToInt16(data, 18),
				BoardNumber = BitConverter.ToUInt16(data, 22),
				TriggerCell = BitConverter.ToUInt16(data, 26),
				EventData = new()
			};

			for (int i = 28; i < data.Length; i += (512 + 2) * 4)
			{
				int ii = i;
				DRS4EventData eventData = new();
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
				
		public static DRS4FileParser NewParser(FileStream stream)
		{
			return new DRS4FileParser(stream);
		}

		public async Task<DRS4FileData> ParseAsync() 
		{
			DRS4FileData Data = new();
			{
				long temp_pos = file.Position;
				file.Position = 3;
				byte versByte = (byte)file.ReadByte();
				file.Position = temp_pos;
				Data.Version = Byte.Parse(Convert.ToChar(versByte).ToString());
			}

			SortedDictionary<long, DRS4FileFlag> DRS4FileFlagDict = BuildDictionary();

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

			List<Task<DRS4Event>> tskEventData = new();
			{
				List<long> eventPos = new();
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
					int read_length = (int)(eventPos[i + 1] - (eventPos[i] + 4) );
					byte[] eventData = new byte[read_length];
					file.Read(eventData, 0, read_length);
					tskEventData.Add(Task.Run(() => ParseEvent(eventData)));
				}
				file.Position = temp_pos;
			}

			Data.Time = await tskTimeData;

			List<DRS4Event> events = new();
			for (int i = 0; i < tskEventData.Count; i++)
			{
				events.Add(await tskEventData[i]);
			}

			Data.Events = events;
			return Data;
		}

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

		private string LineString(byte[] line)
		{
			return $"{Convert.ToChar(line[0])}{Convert.ToChar(line[1])}{Convert.ToChar(line[2])}{Convert.ToChar(line[3])}";
		}
	}
}
