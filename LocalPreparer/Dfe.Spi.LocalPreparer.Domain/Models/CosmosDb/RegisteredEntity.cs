namespace Dfe.Spi.LocalPreparer.Domain.Models.CosmosDb;

public class RegisteredEntity
{
    public string Type { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public LinkedEntity[] Entities { get; set; }
    public Link[] Links { get; set; }
}