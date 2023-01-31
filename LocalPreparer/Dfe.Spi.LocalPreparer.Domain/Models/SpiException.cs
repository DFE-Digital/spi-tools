namespace Dfe.Spi.LocalPreparer.Domain.Models;
public class SpiException : Exception
{
    public List<string> Errors { get; }

    public SpiException() { }

    public SpiException(string message)
        : base(message) { }

    public SpiException(string message, Exception inner)
        : base(message, inner) { }

    public SpiException(List<string> errors, Exception inner)
        : this("Executing the operation failed, please review the following (view log.txt for detailed exception)", inner)
    {
        Errors = errors;
    }
}

