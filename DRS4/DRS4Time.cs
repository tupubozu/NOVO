using System.Collections.Generic;

namespace NovoParser.DRS4
{
    /// <summary>
    /// DRS4Time is a representation of the data found in/after the "TIME"-header inside a DRS4 binary file.
    /// </summary>
    public class DRS4Time
	{
		public ushort BoardNumber;
		public List<DRS4TimeData> TimeData;
	}
}
