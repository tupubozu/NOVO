using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NOVO
{
	static class ParserOptions
	{
		public static bool Trim { get; private set; }
		public static bool Exclude { get; private set; }

		private static readonly char[] forbiddenChars = { ' ', '\t', '\n', '\"', '\''}; 
		static ParserOptions()
		{
			(Trim,Exclude) = (true,true);
		}

		public static void SetOptions(string[] args)
		{
			foreach (string arg in args)
			{
				if(arg.Contains("--exclude="))
				{
					try
					{
						string[] temp = arg.Split('=');
						if (temp[1].Trim(forbiddenChars).ToLowerInvariant().Contains("false"))
						{
							Exclude = false;
						}
						else if (temp[1].Trim(forbiddenChars).ToLowerInvariant().Contains("true"))
						{
							Exclude = true;
						}
					}
					catch (Exception)
					{
						Exclude = true;
					}
				}
				else if (arg.Contains("--trim="))
				{
					try
					{
						string[] temp = arg.Split('=');
						if (temp[1].Trim(forbiddenChars).ToLowerInvariant().Contains("false"))
						{
							Trim = false;
						}
						else if (temp[1].Trim(forbiddenChars).ToLowerInvariant().Contains("true"))
						{
							Trim = true;
						}
					}
					catch (Exception)
					{
						Trim = true;
					}
				}
			}
		}
	}
}
