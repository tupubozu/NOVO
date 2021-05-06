using NOVO.DRS4File;
using NOVO.Waveform;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NOVO
{
	class Program
	{
		static async Task Main(string[] args)
		{
			ParserOptions.SetOptions(args);

			Console.WriteLine("NOVO-project DRS4 binary file parser/reader");
			Console.WriteLine("-------------------------------------------");

			Console.Write("Path to file: ");
			char[] trimChars = { '\"', ' ', '\'' };
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

			if (data.Events.Count > Waves.Count)
			{
				Console.WriteLine("Excluded {0} events due to ADC saturation", data.Events.Count - Waves.Count);

				Console.WriteLine("-------------------------------------------");
			}

			try
			{
				Console.Write("Creating data directory... ");
				Directory.CreateDirectory(
					Path.Combine(
						Path.GetDirectoryName(user_path),
						$"{Path.GetFileNameWithoutExtension(user_path)}_data"
						)
					);
				Console.WriteLine("Done");

			}
			catch (Exception ex)
			{
				Console.WriteLine("Failed\nReason: {0}", ex.Message);
				Console.Error.WriteLine(ex);
			}
			finally
			{
				Console.WriteLine("-------------------------------------------");
			}

			List<IAsyncResult> tskWorker = new();
			foreach (WaveformEvent waveformEvent in Waves)
			{
				tskWorker.Add(Task.Run(async () =>
				   {
					   try
					   {
						   string[] fileString = await waveformEvent.ToCSVAsync(0.1);
						   string fileName = $"DRS4_{waveformEvent.BoardNumber}_{waveformEvent.EventDateTime.ToString("yyyy-MM-dd_HHmmssffff")}_{waveformEvent.SerialNumber}.csv";

						   using var SW = new StreamWriter(
							   path: Path.GetFullPath(
								   Path.Combine(
									   Path.GetDirectoryName(user_path),
									   $"{Path.GetFileNameWithoutExtension(user_path)}_data",
									   fileName
									   )
								   ),
							   append: false,
							   encoding: Encoding.UTF8
							   );

						   foreach (string str in fileString)
						   {
							   SW.WriteLine(str);
						   }
							// Console.Out.WriteLine("Exported file: {0}", fileName);
						}
					   catch (Exception ex)
					   {
						   Console.Error.WriteLine("Thread {0} - ID: {1}\n{2}", Thread.CurrentThread.Name, Thread.CurrentThread.ManagedThreadId, ex);
					   }
				   }));
			}

			await PendingOperationMessage("Processing...", tskWorker);

#if DEBUG
			Console.WriteLine("\n-------------------------------------------\nEnd of program");
			Console.ReadKey(true);
#endif
		}

		static async Task PendingOperationMessage(string message, List<IAsyncResult> tasks)
		{
			string[] rotate = { @"(-)", @"(\)", @"(|)", @"(/)" };
			int cntr = 0;

			Console.CursorVisible = false;

			foreach (var task in tasks)
			{
				while (!task.IsCompleted)
				{
					lock (Console.Out)
					{
						//Console.Out.WriteLine();
						Console.Out.WriteLine($"{message} {rotate[cntr]}");
						(int x, int y) = Console.GetCursorPosition();
						Console.SetCursorPosition(0, y - 1);
						cntr = ++cntr % rotate.Length;
					}
					Thread.Sleep(200);
				}

				await (task as Task); // Necessary?
				(task as IDisposable).Dispose();
			}

			Console.CursorVisible = true;
		}
	}
}
