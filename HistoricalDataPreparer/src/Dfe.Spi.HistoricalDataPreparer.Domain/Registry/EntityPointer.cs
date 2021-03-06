namespace Dfe.Spi.HistoricalDataPreparer.Domain.Registry
{
    public class EntityPointer
    {
        public string EntityType { get; set; }
        public string SourceSystemName { get; set; }
        public string SourceSystemId { get; set; }

        public override string ToString()
        {
            return $"{EntityType}:{SourceSystemName}:{SourceSystemId}";
        }
    }
}