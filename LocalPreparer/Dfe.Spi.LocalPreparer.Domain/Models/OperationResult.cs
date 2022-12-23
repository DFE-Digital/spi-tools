namespace Dfe.Spi.LocalPreparer.Domain.Models;
public class OperationResult
{
    public bool Succeeded { get; set; }

    public List<OperationError> Errors { get; set; } = new List<OperationError>();

}
public class OperationResult<T> : OperationResult
{
    public OperationResult()
    {
    }
    public OperationResult(T model)
    {
        Value = model;
    }
    public T Value { get; set; }
}

