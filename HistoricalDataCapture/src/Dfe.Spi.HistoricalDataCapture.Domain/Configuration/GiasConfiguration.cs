namespace Dfe.Spi.HistoricalDataCapture.Domain.Configuration
{
    public class GiasConfiguration
    {
        public string DownloadSchedule { get; set; }
        
        public string SoapUrl { get; set; }
        public string SoapUsername { get; set; }
        public string SoapPassword { get; set; }
        public int ExtractId { get; set; }
    }
}