namespace Dfe.Spi.HistoricalDataLoader.GiasLoaderConsoleApp.Models
{
    public class LocalAuthority : PointInTimeModelBase
    {
        public const string ManagementGroupType = "LA";
        
        public int Code { get; set; }
        public string Name { get; set; }
    }
}