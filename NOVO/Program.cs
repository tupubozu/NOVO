using System;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using NOVO.DRS4File;

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

			Console.WriteLine("\n-------------------------------------------\nEnd of program");
			Console.ReadKey(true);
		}
	}
}
