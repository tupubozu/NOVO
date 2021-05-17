using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NOVO
{
	/// <summary>
	/// Class to contain program flags and program argument parsing methods
	/// </summary>
	static class ParserOptions
	{
		public static bool Trim { get; private set; }
		public static bool Exclude { get; private set; }
		public static bool ZipOutput { get; private set; }
		public static bool HelpText { get; private set; }

		public static bool Interactive { get; private set; }

		public static readonly char[] TrimChars = { ' ', '\t', '\n', '\"', '\'' };
		static ParserOptions()
		{
			(Trim, Exclude, ZipOutput, HelpText, Interactive) = (true, true, false, false, false);
		}

		/// <summary>
		/// Parses boolean values from program arguments
		/// </summary>
		/// <param name="args">Array of string arguments</param>
		public static void SetOptions(string[] args)
		{
			foreach (string arg in args)
			{
				if(arg.Contains("--exclude="))
				{
					Exclude = GetArgsBool(arg, Exclude);
				}
				else if (arg.Contains("--trim="))
				{
					Trim = GetArgsBool(arg, Trim);
				}
				else if (arg.Contains("--zip="))
				{
					ZipOutput = GetArgsBool(arg, ZipOutput);
				}
				else if (arg.Contains("-i"))
				{
					Interactive = true;
				}
				else if (arg.Contains("--help") || arg.Contains("-h"))
				{
					HelpText = true;
				}
			}
		}

		/// <summary>
		/// General purpose argument parser method
		/// </summary>
		/// <param name="arg">Argument string</param>
		/// <param name="fallback_val">Return value if parsing fails</param>
		/// <returns>Argument as boolean</returns>
		private static bool GetArgsBool(string arg, bool fallback_val)
		{
			bool temp = false;
			try
			{
				temp = bool.Parse(arg);
			}
			catch (Exception)
			{
				temp = fallback_val;
			}
			return temp;
		}

		/// <summary>
		/// Method for parsing valid directory paths from program arguments
		/// </summary>
		/// <param name="args">Program arguments</param>
		/// <returns>List of paths</returns>
		public static List<string> GetFiles(string[] args)
		{
			List<string> temp = new();
			foreach (string arg in args)
			{
				if (File.Exists(arg)) 
				{
					temp.Add(arg);
				}
			}
			return temp;
		}
	}
}
