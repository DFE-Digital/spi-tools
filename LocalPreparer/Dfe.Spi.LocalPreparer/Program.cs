﻿using Dfe.Spi.LocalPreparer.Azure;
using Dfe.Spi.LocalPreparer.Common.Configurations;
using Dfe.Spi.LocalPreparer.Common.Enums;
using Dfe.Spi.LocalPreparer.Common.Presentation;
using Dfe.Spi.LocalPreparer.Common.Utils;
using Dfe.Spi.LocalPreparer.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        Logo.Display();
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine("Dfe.Spi Local Preparer Tool!" + Environment.NewLine, Console.ForegroundColor);
        Console.ResetColor();
        await AuthenticateAsync();
    }

    private static async Task ServiceListAsync()
    {
        var selectedService = SelectService();
        await ServiceSubmenuAsync(selectedService);
    }


    private static ServiceName SelectService()
    {
        var services = Enum.GetValues(typeof(ServiceName)).Cast<ServiceName>()
           .ToDictionary(t => t.ToString(), t => (int)t);

        var menu = new Navigation<int>(services, "Please select a service you would like to configure to run locally!" + Environment.NewLine);
        var selectedOption = menu.Run(true);
        var selectedServiceName = services.FirstOrDefault(x => x.Value == selectedOption);
        Enum.TryParse(selectedServiceName.Key, out ServiceName serviceName);
        return serviceName;
    }

    private static async Task ServiceSubmenuAsync(ServiceName serviceName)
    {
        var submenuItems = CreateServiceSubmenu(serviceName);

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

    private static async Task ExecuteCopySettingFilesAsync(ServiceName serviceName)
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


    private static async Task ExecuteCopyTable(ServiceName serviceName)
    {
        var azureStorageService = IoC.Services.GetService<IAzureStorageService>();
        await azureStorageService.CopyTableToBlobAsync(serviceName);
        await azureStorageService.CopyBlobToTableAsync(serviceName);
        await GoBack("Press any key to continue...!", async () => await ServiceSubmenuAsync(serviceName));

    }

    private static async Task ExecuteCreateQueues(ServiceName serviceName)
    {
        var azureStorageService = IoC.Services.GetService<IAzureStorageService>();
        await azureStorageService.CreateQueuesAsync(serviceName);
        await GoBack("Press any key to continue...!", async () => await ServiceSubmenuAsync(serviceName));
    }

    private static async Task ExecuteCopyBlobs(ServiceName serviceName)
    {
        var azureStorageService = IoC.Services.GetService<IAzureStorageService>();
        await azureStorageService.CopyBlobAsync(serviceName);
        await GoBack("Press any key to continue...!", async () => await ServiceSubmenuAsync(serviceName));
    }


    private static async Task GoBack(string prompt, Func<Task> enterAction)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"{Environment.NewLine}{prompt}{Environment.NewLine}");
        Console.ResetColor();
        Console.ReadKey();
        await enterAction.Invoke();
    }

    private static async Task<bool> AuthenticateAsync()
    {
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine($"{Environment.NewLine + Environment.NewLine}Authenticating with Azure....");
        Console.WriteLine($"{Environment.NewLine}Please check your browser to authenticate using your Azure credentials!");
        Console.ResetColor();
        Thread.Sleep(2000);
        var azureAuthenticationService = IoC.Services.GetService<IAzureAuthenticationService>();
        var context = await azureAuthenticationService.AuthenticateAsync();
        if (context == null) return false;
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Authentication successful!");
        Console.WriteLine($"Active subscription: {context.SubscriptionName} ({context.SubscriptionId})");
        Console.ResetColor();
        await GoBack("Press any key to continue...", async () => await ServiceListAsync());
        return true;
    }

    private static Dictionary<string, int> CreateServiceSubmenu(ServiceName serviceName)
    {
        var submenuItems = new Dictionary<string, int>();
        var _configurations = IoC.Services.GetService<IOptions<SpiSettings>>();

        var tables = _configurations.Value.Services?.GetValueOrDefault(serviceName).Tables;
        var queues = _configurations.Value.Services?.GetValueOrDefault(serviceName).Queues;
        var blobs = _configurations.Value.Services?.GetValueOrDefault(serviceName).BlobContainers;

        submenuItems.Add("Copy setting files", 0);

        if (tables != null && tables.Any())
            submenuItems.Add("Copy Azure Storage tables", 1);
        if (queues != null && queues.Any())
            submenuItems.Add("Copy Azure Storage queues", 2);
        if (blobs != null && blobs.Any())
            submenuItems.Add("Copy Azure Storage blobs", 3);

        submenuItems.Add("Go back", 4);
        return submenuItems;
    }

}