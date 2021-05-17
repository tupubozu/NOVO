using NOVO.DRS4File;
using NOVO.Waveform;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NOVO
{
	class Program
	{
		/// <summary>
		/// Entry point for "NOVO-Parser"
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		static async Task Main(string[] args)
		{
			ParserOptions.SetOptions(args);
			List<string> binaryFiles = ParserOptions.GetFiles(args);

			Task tskProcess;
			if (ParserOptions.Interactive)
			{
				tskProcess = Task.Run(() => InteractiveMode());
			}
			else
			{
				Console.WriteLine("NOVO-project DRS4 binary file parser/reader");
				tskProcess = Task.Run(() =>
				{
				   Task[] workers = new Task[binaryFiles.Count];
				   for (int i = 0; i < binaryFiles.Count; i++)
				   {
					   workers[i] = Task.Run(() => CLIMode(binaryFiles[i]));
				   }

				   Task.WaitAll(workers);
				   foreach (Task worker in workers)
				   {
					   worker.Dispose();
				   }
				});
			}

			PendingOperationMessage("Processing... ", tskProcess);
			await tskProcess;
			tskProcess.Dispose();

#if DEBUG
			Console.WriteLine("\n-------------------------------------------\nEnd of program");
			Console.ReadKey(true);
#endif
		}

		/// <summary>
		/// Method to distract user during processing time...
		/// Indicates that the program is running. 
		/// </summary>
		/// <param name="message">Message to display to the user/in the console</param>
		/// <param name="task">Task being processed</param>
		static void PendingOperationMessage(string message, IAsyncResult task)
		{
			string[] rotate = { @"(-)", @"(\)", @"(|)", @"(/)" };
			int cntr = 0;

			Console.CursorVisible = false;

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

			Console.CursorVisible = true;
		}

#warning Deprecated method: static async Task PendingOperationMessage(string message, List<IAsyncResult> tasks)
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

#warning Refactor: static async Task InteractiveMode()
		/// <summary>
		/// Method handling the "Interactive mode". 
		/// Will query user for information.
		/// </summary>
		/// <returns></returns>
		static async Task InteractiveMode()
		{
			Console.WriteLine("NOVO-project DRS4 binary file parser/reader");
			Console.WriteLine("-------------------------------------------");

			Console.Write("Path to file: ");
			var user_input = Console.ReadLine().Trim(ParserOptions.TrimChars);
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

			//await PendingOperationMessage("Processing...", tskWorker);
		}

		/// <summary>
		/// Method handling the "CLI mode". 
		/// </summary>
		/// <param name="file">Path to DRS4 binary data file</param>
		/// <returns></returns>
		static async Task CLIMode(string file)
		{
			DRS4FileData data = await ReadFile(file);

			if (data != null)
			{
				Console.Out.WriteLine($"{file}\n{data}");
			}
			else return;
			
			Task<List<WaveformEvent>> tskWaves = data.ToWaveformEventsAsync();
			//List<WaveformEvent> Waves = data.ToWaveformEvents();

			string targetPath = ParserOptions.ZipOutput?
				Path.Combine(
				Path.GetDirectoryName(file),
				$"{Path.GetFileNameWithoutExtension(file)}_data.zip"
				):
				Path.Combine(
				Path.GetDirectoryName(file),
				$"{Path.GetFileNameWithoutExtension(file)}_data"
				);

			Task[] workers = GetWorkers(targetPath, await tskWaves);

			Task.WaitAll(workers);
			foreach (Task worker in workers)
			{
				worker.Dispose();
			}
		}

		/// <summary>
		/// Method handling the creation of Task objects responsible for file operations.
		/// </summary>
		/// <param name="targetPath">Path to target directory or target Zip archive</param>
		/// <param name="Waves">Data collection</param>
		/// <returns>Array of Task objects handling async file operations.</returns>
		static Task[] GetWorkers(string targetPath,List<WaveformEvent> Waves)
		{
			List<Task> tskWorker = new();
			try
			{
				if (!ParserOptions.ZipOutput)
				{
					Directory.CreateDirectory(targetPath);
					foreach (WaveformEvent waveformEvent in Waves)
					{
						tskWorker.Add(Task.Run(() => MulitFileOut(waveformEvent, targetPath)));
					}
				}
				else
				{
					using FileStream stream = new FileStream(targetPath, FileMode.Create);
					using ZipArchive archive = new ZipArchive(
						stream: stream,
						mode: ZipArchiveMode.Create,
						leaveOpen: true,
						entryNameEncoding: Encoding.UTF8);
					foreach (WaveformEvent waveformEvent in Waves)
					{
						tskWorker.Add(Task.Run(() => ZipFileOut(waveformEvent, archive)));
					}
				}
			}
			catch (Exception ex)
			{
				Console.Out.WriteLine("Operation failed \nReason: {0}", ex.Message);
				Console.Error.WriteLine(ex);
			}
			return tskWorker.ToArray();
		}

		/// <summary>
		/// Parses DRS4 binary data file into a DRS4FileData object.
		/// </summary>
		/// <param name="filePath">Path to DRS4 binary data file</param>
		/// <returns>DRS4FileData object representing binary file contents.</returns>
		static async Task<DRS4FileData> ReadFile(string filePath)
		{
			DRS4FileData data = null;
			try
			{
				DRS4FileParser parser = null;
				using var fileHandler = File.Open(filePath, FileMode.Open);
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
			return data;
		}

		/// <summary>
		/// Method to handle creation of single CSV file from WaveformEvent object.
		/// Writes result to CSV file.
		/// </summary>
		/// <param name="waveformEvent">Data object</param>
		/// <param name="targetPath">Path to target directory</param>
		/// <returns></returns>
		static async Task MulitFileOut(WaveformEvent waveformEvent, string targetPath)
		{
			try
			{
				using Task<string[]> tskFileString = Task.Run(() => waveformEvent.ToCSVAsync(0.1));

				string fileName = $"DRS4_{waveformEvent.BoardNumber}_{waveformEvent.EventDateTime.ToString("yyyy-MM-dd_HHmmssffff")}_{waveformEvent.SerialNumber}.csv";

				using var SW = new StreamWriter(
					path: Path.GetFullPath(Path.Combine(targetPath, fileName)),
					append: false,
					encoding: Encoding.UTF8
					);
				
				string[] fileString = await tskFileString;
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
		}

		/// <summary>
		/// Method to handle creation of single CSV file from WaveformEvent object.
		/// Writes result to Zip archive.
		/// </summary>
		/// <param name="waveformEvent">Data object</param>
		/// <param name="targetArchive">Path to target Zip archive</param>
		/// <returns></returns>
		static async Task ZipFileOut(WaveformEvent waveformEvent, ZipArchive targetArchive)
		{
			try
			{
				using Task<string[]> tskFileString = Task.Run(() => waveformEvent.ToCSVAsync(0.1));
				
				string fileName = $"DRS4_{waveformEvent.BoardNumber}_{waveformEvent.EventDateTime.ToString("yyyy-MM-dd_HHmmssffff")}_{waveformEvent.SerialNumber}.csv";

				ZipArchiveEntry fileEntry = targetArchive.CreateEntry(fileName, CompressionLevel.Optimal);

				using var SW = new StreamWriter(
					stream: fileEntry.Open(), 
					encoding: Encoding.UTF8
					);

				string[] fileString = await tskFileString;
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
		}
	}
}
