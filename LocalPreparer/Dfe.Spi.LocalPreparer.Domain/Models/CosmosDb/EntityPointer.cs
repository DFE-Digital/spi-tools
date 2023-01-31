namespace Dfe.Spi.LocalPreparer.Domain.Models.CosmosDb;

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
