using System;

namespace Dfe.Spi.HistoricalDataLoader.UkrlpLoaderConsoleApp.Models
{
    public class PointInTimeModelBase
    {
        public DateTime PointInTime { get; set; }
        public bool IsCurrent { get; set; }
    }
}