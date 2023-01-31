namespace Dfe.Spi.LocalPreparer.Domain.Models.CosmosDb;
public interface IItem
{
    string Id { get; set; }
    string PartitionableId { get; set; }

}

