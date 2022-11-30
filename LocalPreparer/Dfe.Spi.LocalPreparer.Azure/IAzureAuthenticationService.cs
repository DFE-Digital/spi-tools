using Dfe.Spi.LocalPreparer.Common.Model;

namespace Dfe.Spi.LocalPreparer.Azure;

public interface IAzureAuthenticationService
{
    Task<AzureClientContext?> AuthenticateAsync();
}