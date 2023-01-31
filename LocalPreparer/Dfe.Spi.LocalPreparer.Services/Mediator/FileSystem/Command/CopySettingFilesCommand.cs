using Dfe.Spi.LocalPreparer.Common;
using Dfe.Spi.LocalPreparer.Domain.Models;
using Dfe.Spi.LocalPreparer.Services.Mediator.FileSystem.Query;
using MediatR;

namespace Dfe.Spi.LocalPreparer.Services.Mediator.FileSystem.Command;

public class CopySettingFilesCommand : IRequest<bool>
{
    private readonly string _solutionPath;
    public CopySettingFilesCommand(string solutionPath)
    {
        _solutionPath = solutionPath;
    }

    public class Handler : IRequestHandler<CopySettingFilesCommand, bool>
    {
        private readonly IContextManager _contextManager;
        private readonly IMediator _mediator;

        public Handler(IContextManager contextManager, IMediator mediator)
        {
            _contextManager = contextManager;
            _mediator = mediator;
        }

        public async Task<bool> Handle(CopySettingFilesCommand request,
            CancellationToken cancellationToken)
        {

            try
            {
                var serviceNameValidationResult = await _mediator.Send(new ValidateServiceNameQuery(_contextManager.Context.ActiveService, request._solutionPath), cancellationToken);
                var (projectName, projectPath) = serviceNameValidationResult;

                await Task.Run(() =>
                {
                    if (string.IsNullOrEmpty(projectName) || string.IsNullOrEmpty(projectPath))
                    {
                        throw new SpiException(new List<string>()
                        {
                            "Functions project file not found, please make sure you have selected correct service and provided a path to the solution folder!",
                        }, null);
                    }

                    var settingsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Configurations", "SettingFiles", projectName.Replace(".csproj", ""));
                    var localSettings = Directory.GetFiles(settingsDirectory, "local.settings.json").FirstOrDefault();
                    var launchSettings = Directory.GetFiles(settingsDirectory, "launchSettings.json").FirstOrDefault();

                    File.Copy(localSettings, Path.Combine(projectPath, Path.GetFileName(localSettings)), true);

                    System.IO.Directory.CreateDirectory(Path.Combine(projectPath, "Properties"));
                    File.Copy(launchSettings, Path.Combine(projectPath, "properties", Path.GetFileName(launchSettings)), true);

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Setting files copied successfully!");
                    Console.ResetColor();
                    Thread.Sleep(2000);
                    return true;

                }, cancellationToken);
            }
            catch (Exception e)
            {
                if (e is SpiException spiException)
                {
                    throw new SpiException(spiException.Errors, e);
                }
                throw new SpiException(new List<string>()
                {
                    "Failed to copy setting files",
                }, e);
            }
            return false;
        }
    }
}