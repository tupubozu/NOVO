using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using NOVO.DRS4File;
using NOVO.Waveform;

namespace NOVO
{
	class Program
	{
		static async Task Main(string[] args)
		{
			Console.WriteLine("NOVO-project DRS4 binary file parser/reader");
			Console.WriteLine("-------------------------------------------");
			
			Console.Write("Path to file: ");
			char[] trimChars = {'\"', ' ', '\'' };
			var user_input = Console.ReadLine().Trim(trimChars);
			var user_path = Path.GetFullPath(user_input);

			DRS4FileData data = null;

			try
			{
				DRS4FileParser parser = null;
				using var fileHandler = File.Open(user_path, FileMode.Open);
				parser = DRS4FileParser.NewParser(fileHandler);
				if (parser.Validate())
				{
					data = await parser.ParseAsync();
				}
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine(ex.ToString());
			}

			if (data != null)
			{
				Console.WriteLine("-------------------------------------------");
				Console.WriteLine(data);
			}

			Console.WriteLine("-------------------------------------------");
			Console.Write("Constructing Waveform object... ");
			List<WaveformEvent> Waves = await data.ToWaveformEventsAsync();
			//List<WaveformEvent> Waves = data.ToWaveformEvents();
			Console.WriteLine("Done!");

			Console.WriteLine("-------------------------------------------");

			List<Task<string>> tsk_csv = new();
			foreach(WaveformEvent waveformEvent in Waves)
			{
				tsk_csv.Add(Task.Run(() => waveformEvent.ToCSV(-1.0, 1024.0, 0.1)));
			}

			PendingOperationMessage("Awaiting completion of CSV string generation", tsk_csv);

			List<string> csv_strings = new();
			foreach (var task in tsk_csv)
			{
				csv_strings.Add(task.Result);
			}

			List<Task> tskIO = new();
			for (int i = 0; i < csv_strings.Count; i++)
			{
				WaveformEvent temp_wave = Waves[i];
				string temp_str = csv_strings[i];
				tskIO.Add(Task.Run( () => 
				{
					try
					{
						using var SW = new StreamWriter(
							path: Path.GetFullPath($"DRS4-{temp_wave.BoardNumber}-{temp_wave.EventDateTime}-{temp_wave.SerialNumber}.csv"),
							append: false,
							encoding: Encoding.UTF8,
							bufferSize: temp_str.Length
							);
						SW.Write(temp_str);
					}
					catch (Exception ex)
					{
						Console.Error.WriteLine("Thread {0} - ID: {1}\n{2}", Thread.CurrentThread.Name, Thread.CurrentThread.ManagedThreadId, ex);
					}
					
				}));
			}

			PendingOperationMessage("Awaiting completion of file operations", tskIO);
			
			Console.WriteLine("\n-------------------------------------------\nEnd of program");
			Console.ReadKey(true);
		}

		static void PendingOperationMessage(string message, List<Task<string>> tasks)
		{
			Console.Write(message);
			Console.CursorVisible = false;

			(int x, int y) = Console.GetCursorPosition();
			foreach (var task in tasks)
			{
				byte cntr = 0;
				while (!task.IsCompleted)
				{
					Console.Write(".  ");
					Thread.Sleep(200);
					if (cntr > 2)
					{
						cntr = 0;
						Console.SetCursorPosition(x, y);
						Console.Write("   ");
						Console.SetCursorPosition(x, y);
					}
					else
					{
						Console.SetCursorPosition(x + cntr, y);
						cntr++;
					}
				}
				Console.SetCursorPosition(x, y);
			}

			Console.WriteLine("\tDone!");
			Console.CursorVisible = true;
		}

		static void PendingOperationMessage(string message, List<Task> tasks)
		{
			Console.Write(message);
			Console.CursorVisible = false;

			(int x, int y) = Console.GetCursorPosition();
			foreach (var task in tasks)
			{
				byte cntr = 0;
				while (!task.IsCompleted)
				{
					Console.Write(".  ");
					Thread.Sleep(200);
					if (cntr > 2)
					{
						cntr = 0;
						Console.SetCursorPosition(x, y);
						Console.Write("   ");
						Console.SetCursorPosition(x, y);
					}
					else
					{
						Console.SetCursorPosition(x + cntr, y);
						cntr++;
					}
				}
				Console.SetCursorPosition(x, y);
			}

			Console.WriteLine("\tDone!");
			Console.CursorVisible = true;
		}
	}
}
