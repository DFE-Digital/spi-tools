namespace Dfe.Spi.HistoricalDataPreparer.Domain.Gias
{
    public class GiasDayData
    {
        public Establishment[] Establishments { get; set; }
        public LocalAuthority[] LocalAuthorities { get; set; }
        public Group[] Groups { get; set; }
        public GroupLink[] GroupLinks { get; set; }
    }
}