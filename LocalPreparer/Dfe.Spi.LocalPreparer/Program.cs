using Dfe.Spi.LocalPreparer.Common.Enums;
using Dfe.Spi.LocalPreparer.Common.Presentation;
using Dfe.Spi.LocalPreparer.Common.Utils;
using Dfe.Spi.LocalPreparer.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Dfe.Spi.LocalPreparer;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var configuration = Configure();
        IoC.ConfigureServices(configuration);
        ConfigureLogger();
        await InitAsync();
    }

    private static void ConfigureLogger()
    {
        var logFilePath = Path.Combine(AppContext.BaseDirectory, "log.txt");
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .WriteTo.File(logFilePath)
            .CreateLogger();

        var loggerFactory = IoC.Services.GetRequiredService<ILoggerFactory>();
        loggerFactory.AddSerilog();
    }

    public static IConfiguration Configure()
    {
        var builder = new ConfigurationBuilder()
         .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
         .AddEnvironmentVariables();
        return builder.Build();
    }

    private static async Task InitAsync()
    {
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine($@"
  _____   __        _____       _ 
 |  __ \ / _|      / ____|     (_)
 | |  | | |_ ___  | (___  _ __  _ 
 | |  | |  _/ _ \  \___ \| '_ \| |
 | |__| | ||  __/_ ____) | |_) | |
 |_____/|_| \___(_)_____/| .__/|_|
                         | |      
                         |_|          ver{StringExtensions.GetAppVersion()}", Console.ForegroundColor);

        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine("Welcome to Dfe.Spi Local Preparer Tool!" + Environment.NewLine, Console.ForegroundColor);
        Console.ResetColor();
        Thread.Sleep(2000);
        await ServiceListAsync();
    }

    public static async Task ServiceListAsync()
    {
        var selectedService = SelectService();
        await ServiceSubmenuAsync(selectedService);
    }


    public static ServiceName SelectService()
    {
        var services = Enum.GetValues(typeof(ServiceName)).Cast<ServiceName>()
           .ToDictionary(t => t.ToString(), t => (int)t);

        var menu = new Navigation<int>(services, "Please select a service you would like to configure to run locally!" + Environment.NewLine);
        var selectedOption = menu.Run(true);
        var selectedServiceName = services.FirstOrDefault(x => x.Value == selectedOption);
        Enum.TryParse(selectedServiceName.Key, out ServiceName serviceName);
        return serviceName;
    }

    public static async Task ServiceSubmenuAsync(ServiceName serviceName)
    {
        var submenuItems = new Dictionary<string, int>
            {
                { "Copy setting files", 0 },
                { "Copy Azure Storage tables", 1 },
                { "Copy Azure Storage queues", 2 },
                { "Copy Azure Storage blobs", 3 },
                { "Go back", 4 }
            };

        var subMenu = new Navigation<int>(submenuItems, $"Please select an operation for service: {serviceName}!");
        var selectedItem = subMenu.Run(true);

        switch (selectedItem)
        {
            case 0:
                await ExecuteCopySettingFilesAsync(serviceName);
                break;
            case 1:
                await ExecuteCopyTable(serviceName);
                break;
            case 2:
                await ExecuteCreateQueues(serviceName);
                break;
            case 3:
                await ExecuteCopyBlobs(serviceName);
                break;
            case 4:
                await ServiceListAsync();
                break;
        }
    }

    public static async Task ExecuteCopySettingFilesAsync(ServiceName serviceName)
    {
        var fileSystemService = IoC.Services.GetService<IFileSystemService>();

        var solutionPath = Interactions.Input<string>("Please enter full path to the solution folder. Note: This operation will overwrite your project's local.settings.json and launchSettings.json", null, false, false, ConsoleColor.Blue, null);

        var (projectName, projectPath) = fileSystemService.ValidateServiceName(serviceName, solutionPath);

        if (string.IsNullOrEmpty(projectName))
        {
            Interactions.RaiseError(new List<string>() { "Functions project file not found, please make sure you have selected correct service and provided a path to the solution folder!" }, () => ServiceListAsync());
        }
        fileSystemService.CopySettingFiles(projectName, projectPath);

        await GoBack("Press any key to continue...!", async () => await ServiceSubmenuAsync(serviceName));
    }


    public static async Task ExecuteCopyTable(ServiceName serviceName)
    {
        var azureStorageService = IoC.Services.GetService<IAzureStorageService>();
        await azureStorageService.CopyTableToBlobAsync(serviceName);
        await azureStorageService.CopyBlobToTableAsync(serviceName);
        await GoBack("Press any key to continue...!", async () => await ServiceSubmenuAsync(serviceName));

    }

    public static async Task ExecuteCreateQueues(ServiceName serviceName)
    {
        var azureStorageService = IoC.Services.GetService<IAzureStorageService>();
        await azureStorageService.CreateQueuesAsync(serviceName);
        await GoBack("Press any key to continue...!", async () => await ServiceSubmenuAsync(serviceName));
    }

    public static async Task ExecuteCopyBlobs(ServiceName serviceName)
    {
        var azureStorageService = IoC.Services.GetService<IAzureStorageService>();
        await azureStorageService.CopyBlobAsync(serviceName);
        await GoBack("Press any key to continue...!", async () => await ServiceSubmenuAsync(serviceName));
    }


    public static async Task GoBack(string prompt, Func<Task> enterAction)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"{Environment.NewLine}{prompt}{Environment.NewLine}");
        Console.ResetColor();
        Console.ReadKey();
        await enterAction.Invoke();
    }



}