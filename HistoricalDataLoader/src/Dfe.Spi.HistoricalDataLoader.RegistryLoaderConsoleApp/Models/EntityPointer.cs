namespace Dfe.Spi.HistoricalDataLoader.RegistryLoaderConsoleApp.Models
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