namespace NovoParser.Waveform
{
    /// <summary>
    /// Represents an individual sample of a waveform.
    /// </summary>
    public class WaveformSample
	{
		private double timeComponent;
		public double TimeComponent
		{
			get => timeComponent;
			private set => timeComponent = value;
		}
		public double VoltageComponent { get; private set; }

		public WaveformSample() : this(0.0, 0.0) { }
		public WaveformSample(double time, double voltage) => (TimeComponent, VoltageComponent) = (time, voltage);

		public void ShiftTime(double timeDifference) => timeComponent += timeDifference;
	}
}
