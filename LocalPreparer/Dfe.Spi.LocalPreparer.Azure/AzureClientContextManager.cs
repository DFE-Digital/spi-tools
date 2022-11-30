using Dfe.Spi.LocalPreparer.Common.Model;

namespace Dfe.Spi.LocalPreparer.Azure;

public class AzureClientContextManager : IAzureClientContextManager
{
    private AzureClientContext _azureClientContext;

    public AzureClientContext AzureClientContext => _azureClientContext;

    public void SetContext(AzureClientContext context)
    {
        _azureClientContext = context;
    }
}


