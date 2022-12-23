namespace Dfe.Spi.LocalPreparer.Domain.Models;

public class OperationError
{
    public OperationError(Exception e)
    {
        Code = e.GetType().Name;
        Description = e.Message;
    }
    public OperationError()
    {

    }
    public string Code { get; set; }
    public string Description { get; set; }
}