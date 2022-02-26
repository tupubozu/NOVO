namespace NovoParser.DRS4
{
    /// <summary>
    /// DRS4TimeData a representation of the channel spesific information found after the "TIME"-header inside a DRS4 binary file.
    /// </summary>
    public class DRS4TimeData
	{
		public byte ChannelNumber;
		public float[] Data;
	}
}
