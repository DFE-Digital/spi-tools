using Dfe.Spi.LocalPreparer.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Dfe.Spi.LocalPreparer.Services;
public class FileSystemService : IFileSystemService
{

    private readonly ILogger<FileSystemService> _logger;
    private const string ServiceProjectName = "Dfe.Spi.{0}.Function";

    public FileSystemService(ILogger<FileSystemService> logger)
    {
        _logger = logger;
    }

    public (string?, string?) ValidateServiceName(ServiceName serviceName, string solutionPath)
    {
        try
        {
            var projectFolderName = string.Format(ServiceProjectName, serviceName);
            var dirs = Directory.GetDirectories(solutionPath, $"*{projectFolderName}*", SearchOption.TopDirectoryOnly);
            if (dirs.Length <= 0)
                return (null, null);
            var filePath = System.IO.Directory.GetFiles(dirs[0], "*.csproj")
                 .FirstOrDefault(x => Path.GetExtension(x).ToLower() == ".csproj");
            if (string.IsNullOrEmpty(filePath))
                return (null, null);
            if (filePath.IndexOf(projectFolderName, StringComparison.Ordinal) <= -1) return (null, null);
            var filename = Path.GetFileName(filePath);
            return (filename, filePath.Replace(filename, ""));
        }
        catch (Exception e)
        {
            _logger.LogError($"{nameof(ValidateServiceName)} > Exception: {e}");
            return (null, null);
        }
    }


    public bool CopySettingFiles(string filename, string projectFolder)
    {
        if (string.IsNullOrEmpty(filename) || string.IsNullOrEmpty(projectFolder))
            return false;
        var settingsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Configurations", "SettingFiles", filename.Replace(".csproj", ""));
        var localSettings = Directory.GetFiles(settingsDirectory, "local.settings.json").FirstOrDefault();
        var launchSettings = Directory.GetFiles(settingsDirectory, "launchSettings.json").FirstOrDefault();

        File.Copy(localSettings, Path.Combine(projectFolder, Path.GetFileName(localSettings)), true);

        System.IO.Directory.CreateDirectory(Path.Combine(projectFolder, "Properties"));
        File.Copy(launchSettings, Path.Combine(projectFolder, "properties", Path.GetFileName(launchSettings)), true);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Setting files copied successfully!");
        Console.ResetColor();
        Thread.Sleep(2000);
        return true;
    }
}
