namespace NovoParser.DRS4
{
    /// <summary>
    /// DRS4EventData a representation of the channel spesific information found after the "EHDR"-header inside a DRS4 binary file.
    /// </summary>
    public class DRS4EventData
	{
		public byte ChannelNumber;
		public int Scaler;
		public ushort[] Voltage;
	}
}
