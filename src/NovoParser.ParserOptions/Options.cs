using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovoParser.ParserOptions
{
	/// <summary>
	/// Class to contain program flags and program argument parsing methods
	/// </summary>
	public static class Options
	{
		public static bool Trim { get; private set; }
		public static bool TrimEnd { get; private set; }
		public static bool Exclude { get; private set; }
		public static bool ZipOutput { get; private set; }
		public static bool HelpText { get; private set; }
		public static bool RegularTime { get; private set; }
		public static bool Interactive { get; private set; }
		public static double ThresholdVoltage { get; private set; }
		public static double RegularTimeInterval { get; private set; }

		public static readonly char[] TrimChars = { ' ', '\t', '\n', '\"', '\'' };
		static Options()
		{
			(Trim, Exclude, ZipOutput, HelpText, Interactive) = (true, true, true, false, false);
			TrimEnd = false;
			RegularTime = false;
			RegularTimeInterval = 0.1;
			ThresholdVoltage = 50.0;
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
					Exclude = GetArgsBool(arg.Split('=')[1], Exclude);
				}
				else if (arg.Contains("--trim="))
				{
					Trim = GetArgsBool(arg.Split('=')[1], Trim);
				}
				else if (arg.Contains("--trim-end="))
				{
					TrimEnd = GetArgsBool(arg.Split('=')[1], TrimEnd);
				}
				else if (arg.Contains("--zip="))
				{
					ZipOutput = GetArgsBool(arg.Split('=')[1], ZipOutput);
				}
				else if (arg.Contains("--reg-time="))
				{
					RegularTime = GetArgsBool(arg.Split('=')[1], RegularTime);
				}
				else if (arg.Contains("--threshold="))
				{
					ThresholdVoltage = GetArgsDouble(arg.Split('=')[1], ThresholdVoltage);
				}
				else if (arg.Contains("--reg-time-interval="))
				{
					RegularTimeInterval = GetArgsDouble(arg.Split('=')[1], RegularTimeInterval);
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
		/// General purpose argument parser method
		/// </summary>
		/// <param name="arg">Argument string</param>
		/// <param name="fallback_val">Return value if parsing fails</param>
		/// <returns>Argument as double</returns>
		private static double GetArgsDouble(string arg, double fallback_val)
		{
			double temp = 0.0;
			try
			{
				temp = double.Parse(arg);
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
			List<string> temp = new List<string>();
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
