using Dfe.Spi.LocalPreparer.Azure;
using Dfe.Spi.LocalPreparer.Azure.Authentication;
using Dfe.Spi.LocalPreparer.Azure.CosmosDb;
using Dfe.Spi.LocalPreparer.Azure.CosmosDb.Repositories;
using Dfe.Spi.LocalPreparer.Common;
using Dfe.Spi.LocalPreparer.Domain.Models;
using Dfe.Spi.LocalPreparer.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dfe.Spi.LocalPreparer;

public static class IoC
{
    public static IServiceProvider Services { get; private set; }
    public static void ConfigureServices(IConfiguration configuration)
    {
        IServiceCollection services = new ServiceCollection();
        services.AddTransient<IFileSystemService, FileSystemService>();
        services.AddTransient<IAzureStorageService, AzureStorageService>();
        services.AddTransient<IAzureAuthenticationService, AzureAuthenticationService>();
        services.AddSingleton<IContextManager, ContextManager>();
        services.AddTransient<IAzureCosmosService, AzureCosmosService>();
        services.AddSingleton<ICosmosLocalClientProvider, CosmosLocalClientProvider>();
        services.AddSingleton<ICosmosRemoteClientProvider, CosmosRemoteClientProvider>();
        services.AddTransient<ICosmosContainerProvider, CosmosContainerProvider>();
        services.AddTransient<ILocalCosmosRepository, LocalCosmosRepository>();
        services.AddTransient<IRemoteCosmosRepository, RemoteCosmosRepository>();


        services.Configure<SpiSettings>(configuration.GetSection("SpiSettings"));

        services.AddOptions();
        services.AddLogging();
        Services = services.BuildServiceProvider();
    }
}
