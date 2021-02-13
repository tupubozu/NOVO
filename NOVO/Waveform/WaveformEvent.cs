using System;
using System.Collections.Generic;

namespace NOVO.Waveform
{
	class WaveformEvent
	{
		public DateTime EventDateTime;
		public byte BoardNumber;

		public List<WaveformData> Channels;

		public void NormalizeTime()
		{
			throw new NotImplementedException();
		}
	}
}
