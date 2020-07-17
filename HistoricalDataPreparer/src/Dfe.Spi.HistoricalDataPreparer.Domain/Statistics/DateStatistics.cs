using System;

namespace Dfe.Spi.HistoricalDataPreparer.Domain.Statistics
{
    public class DateStatistics
    {
        public DateTime Date { get; set; }
        public TimeSpan Duration { get; set; }
        
        public int EstablishmentsChanged { get; set; }
        public int GroupsChanged { get; set; }
        public int LocalAuthoritiesChanged { get; set; }
        public int ProvidersChanged { get; set; }
    }
}