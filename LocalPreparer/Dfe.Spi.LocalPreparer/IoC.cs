using Dfe.Spi.LocalPreparer.Common.Configurations;
using Dfe.Spi.LocalPreparer.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dfe.Spi.LocalPreparer
{
    public static class IoC
    {
        public static IServiceProvider Services { get; private set; }
        public static void ConfigureServices(IConfiguration configuration)
        {
            IServiceCollection services = new ServiceCollection();
            services.AddTransient<IFileSystemService, FileSystemService>();
            services.AddTransient<IAzureStorageService, AzureStorageService>();
            
            services.Configure<SpiSettings>(configuration.GetSection("SpiSettings"));

            services.AddOptions();
            services.AddLogging();
            Services = services.BuildServiceProvider();
        }
    }
}
