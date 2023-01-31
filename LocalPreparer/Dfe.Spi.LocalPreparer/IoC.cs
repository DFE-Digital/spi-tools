using System.Reflection;
using Dfe.Spi.LocalPreparer.Azure;
using Dfe.Spi.LocalPreparer.Azure.Authentication;
using Dfe.Spi.LocalPreparer.Azure.AzureStorage;
using Dfe.Spi.LocalPreparer.Azure.CosmosDb;
using Dfe.Spi.LocalPreparer.Azure.CosmosDb.Repositories;
using Dfe.Spi.LocalPreparer.Common;
using Dfe.Spi.LocalPreparer.Domain.Models;
using Dfe.Spi.LocalPreparer.Services;
using Dfe.Spi.LocalPreparer.Services.Mediator.Cosmos.Command;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dfe.Spi.LocalPreparer;

public static class IoC
{
    public static IServiceProvider Services { get; private set; }
    public static IMediator Mediator { get; private set; }
    public static void ConfigureServices(IConfiguration configuration)
    {
        IServiceCollection services = new ServiceCollection();
        services.AddTransient<IAzureStorageClientService, AzureStorageClientService>();
        services.AddTransient<IAzureAuthenticationService, AzureAuthenticationService>();
        services.AddSingleton<IContextManager, ContextManager>();
        services.AddSingleton<ICosmosLocalClientProvider, CosmosLocalClientProvider>();
        services.AddSingleton<ICosmosRemoteClientProvider, CosmosRemoteClientProvider>();
        services.AddTransient<ICosmosContainerProvider, CosmosContainerProvider>();
        services.AddTransient<ILocalCosmosRepository, LocalCosmosRepository>();
        services.AddTransient<IRemoteCosmosRepository, RemoteCosmosRepository>();

        services.AddMediatR(Assembly.GetExecutingAssembly(), Assembly.GetAssembly(typeof(CopyCosmosDataCommand)));

        services.Configure<SpiSettings>(configuration.GetSection("SpiSettings"));

        services.AddOptions();
        services.AddLogging();
        Services = services.BuildServiceProvider();
        Mediator = Services.GetService<IMediator>();
    }
}
