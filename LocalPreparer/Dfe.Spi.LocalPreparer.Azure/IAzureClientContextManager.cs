using Dfe.Spi.LocalPreparer.Common.Model;

namespace Dfe.Spi.LocalPreparer.Azure;

public interface IAzureClientContextManager
{
    AzureClientContext AzureClientContext { get; }
    void SetContext(AzureClientContext context);
}