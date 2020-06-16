namespace Dfe.Spi.HistoricalDataCapture.Domain.Configuration
{
    public class HistoricalDataCaptureConfiguration
    {
        public GiasConfiguration Gias { get; set; }
        public StorageConfiguration Storage { get; set; }
    }
}