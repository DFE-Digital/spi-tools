namespace Dfe.Spi.HistoricalDataPreparer.Domain.Gias
{
    public class LocalAuthority
    {
        public const string ManagementGroupType = "LA";
        
        public int Code { get; set; }
        public string Name { get; set; }
    }
}