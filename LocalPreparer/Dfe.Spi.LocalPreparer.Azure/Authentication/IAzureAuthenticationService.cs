using Dfe.Spi.LocalPreparer.Domain.Models;

namespace Dfe.Spi.LocalPreparer.Azure.Authentication;

public interface IAzureAuthenticationService
{
    Task<Context?> AuthenticateAsync();
}