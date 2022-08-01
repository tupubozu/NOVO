using NovoParser.DRS4;
using NovoParser.Waveform;
using NovoParser.ParserOptions;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NovoParser.CLI.UserInterface;

namespace NOVO
{
	class Program
	{
		/// <summary>
		/// Entry point for "NOVO-Parser"
		/// </summary>
		/// <param name="args">Array of program argument strings</param>
		/// <returns></returns>
		static async Task Main(string[] args)
		{
			Options.SetOptions(args);
			List<string> binaryFiles = Options.GetFiles(args);

			var assemblyName = typeof(Program).Assembly.GetName();
			Console.WriteLine(UIStrings.ProgramNameVersionReport, assemblyName.Name, assemblyName.Version); // NOVO-project DRS4 binary file parser/reader

			if (Options.HelpText)
			{
				Console.WriteLine(UIStrings.HelpText);
			}
			else if (Options.Interactive)
			{
				using Task tskProcess = InteractiveMode();
				await tskProcess;
			}
			else
			{
				if (binaryFiles.Count > 0)
				{

					List<Task> workers = new();
					for (int i = 0; i < binaryFiles.Count; i++)
					{
						int alias_i = i;
						workers.Add(Task.Run(() => CLIMode(binaryFiles[alias_i])));
					}

					using Task tskProcess = Task.WhenAll(workers.ToArray());
					PendingOperationMessage(UIStrings.ProcessingIdleText, tskProcess);
					await tskProcess;

					foreach (Task worker in workers)
					{
						await worker;
						worker.Dispose();
					}
				}
				else
				{
					Console.WriteLine(UIStrings.NoFilesErrorText);
				}
				
			}

#if DEBUG
			Console.WriteLine(UIStrings.SeparatorLineText);
			Console.WriteLine(UIStrings.ProgramEndText);
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

			bool loopEnd = task.IsCompleted;
			while (!loopEnd)
			{
				lock (Console.Out)
				{
					Console.CursorVisible = false;
					//Console.Out.WriteLine();
					Console.Out.WriteLine($"{message} {rotate[cntr]}");
					(int x, int y) = Console.GetCursorPosition();
					Console.SetCursorPosition(0, y - 1);
					cntr = ++cntr % rotate.Length;

					loopEnd = task.IsCompleted;
					if (loopEnd)
					{
						Console.SetCursorPosition(message.Length + 1, y - 1);
						Console.WriteLine(UIStrings.JobDoneText);
					}
				}
				Thread.Sleep(200);
			}

			Console.CursorVisible = true;
		}

		/// <summary>
		/// Method handling the "Interactive mode". 
		/// Will query user for information.
		/// </summary>
		/// <returns></returns>
		static async Task InteractiveMode()
		{
			Console.WriteLine(UIStrings.ProgramExplainationText);
			Console.WriteLine(UIStrings.SeparatorLineText);

			Console.Write(UIStrings.FilePathUserQueryText);
			var user_input = Console.ReadLine().Trim(Options.TrimChars);
			var user_path = Path.GetFullPath(user_input);

			DRS4FileData data = await ReadFile(user_path);

			if (data != null)
			{
				Console.WriteLine(UIStrings.SeparatorLineText);
				Console.WriteLine(data);
			}

			Console.WriteLine(UIStrings.SeparatorLineText);
			var waveOperation = data.ToWaveformEventsAsync();
			PendingOperationMessage(UIStrings.WaveformCreationText, waveOperation);
			List<WaveformEvent> Waves = await waveOperation;
			//List<WaveformEvent> Waves = data.ToWaveformEvents();

			Console.WriteLine(UIStrings.SeparatorLineText);

			if (data.Events.Count > Waves.Count)
			{
				int temp = data.Events.Count - Waves.Count;
				Console.WriteLine(UIStrings.EventADCExclucionReport, temp, temp > 1 ? UIStrings.EventNamePlural : UIStrings.EventNameSingular);

				Console.WriteLine(UIStrings.SeparatorLineText);
			}

			using Task tskTemp = Task.Run(() => RunWorkers(user_path,Waves));
			PendingOperationMessage(UIStrings.ProcessingIdleText, tskTemp);
			await tskTemp;
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
				Console.Out.WriteLine($"{file}\n{data}\n");
			}
			else return;
			
			Task<List<WaveformEvent>> tskWaves = data.ToWaveformEventsAsync();
			//List<WaveformEvent> Waves = data.ToWaveformEvents();

			string targetPath = Options.ZipOutput?
				Path.Combine(
					Path.GetDirectoryName(file),
					$"{Path.GetFileNameWithoutExtension(file)}_data.zip"
				):
				Path.Combine(
					Path.GetDirectoryName(file),
					$"{Path.GetFileNameWithoutExtension(file)}_data"
				);
			
			List<WaveformEvent> Waves = await tskWaves;
			if (data.Events.Count > Waves.Count)
			{
				int temp = data.Events.Count - Waves.Count;
				Console.WriteLine("{2}\n" + UIStrings.EventADCExclucionReport + "\n", temp, (temp > 1) ? UIStrings.EventNamePlural : UIStrings.EventNameSingular, file);
			}

			RunWorkers(targetPath, Waves);
		}

		/// <summary>
		/// Method handling the creation and execution of Task objects responsible for file operations.
		/// </summary>
		/// <param name="targetPath">Path to target directory or target Zip archive</param>
		/// <param name="Waves">Data collection</param>
		static void RunWorkers(string targetPath,List<WaveformEvent> Waves)
		{
			List<Task> tskWorker = new();
			try
			{
				if (!Options.ZipOutput)
				{
					Directory.CreateDirectory(targetPath);
					foreach (WaveformEvent waveformEvent in Waves)
					{
						tskWorker.Add(Task.Run(() => MulitFileOut(waveformEvent, targetPath)));
					}
					Task.WaitAll(tskWorker.ToArray());
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
					Task.WaitAll(tskWorker.ToArray());
				}
			}
			catch (Exception ex)
			{
				Console.Out.WriteLine(UIStrings.OperationFailedReport, ex.Message);
#if DEBUG
				Console.Error.WriteLine(ex);
#endif
			}
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
				using var fileHandler = File.Open(filePath, FileMode.Open);
				DRS4FileParser parser = DRS4FileParser.NewParser(fileHandler);
				if (parser.Validate())
				{
					data = await parser.ParseAsync();
				}
			}
			catch (Exception ex)
			{
#if DEBUG
				Console.Error.WriteLine(ex);
#else
				Console.Error.WriteLine(ex.Message);
#endif
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
#if DEBUG
				Console.Error.WriteLine(UIStrings.ThreadExceptionReport, Thread.CurrentThread.Name, Thread.CurrentThread.ManagedThreadId, ex);
#else
				Console.Error.WriteLine(UIStrings.ThreadExceptionReport, Thread.CurrentThread.Name, Thread.CurrentThread.ManagedThreadId, ex.Message);
#endif
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
				Task<string[]> tskFileString = Options.RegularTime? waveformEvent.ToCSVAsync(Options.RegularTimeInterval): waveformEvent.ToCSVAsync();

				string fileName = $"DRS4_{waveformEvent.BoardNumber}_{waveformEvent.EventDateTime.ToString("yyyy-MM-dd_HHmmssffff")}_{waveformEvent.SerialNumber}.csv";

				string[] fileString = await tskFileString;

				lock (targetArchive)
				{
					ZipArchiveEntry fileEntry = targetArchive.CreateEntry(fileName, CompressionLevel.Optimal);

					using var SW = new StreamWriter(
						stream: fileEntry.Open(),
						encoding: Encoding.UTF8
						);

					foreach (string str in fileString)
					{
						SW.WriteLine(str);
					}
				}

				// Console.Out.WriteLine("Exported file: {0}", fileName);
			}
			catch (Exception ex)
			{
#if DEBUG
				Console.Error.WriteLine(UIStrings.ThreadExceptionReport, Thread.CurrentThread.Name, Thread.CurrentThread.ManagedThreadId, ex);
#else
				Console.Error.WriteLine(UIStrings.ThreadExceptionReport, Thread.CurrentThread.Name, Thread.CurrentThread.ManagedThreadId, ex.Message);
#endif
			}
		}
	}
}
