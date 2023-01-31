using Dfe.Spi.LocalPreparer.Domain.Enums;
using Dfe.Spi.LocalPreparer.Domain.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Dfe.Spi.LocalPreparer.Services.Mediator.FileSystem.Query;
public class ValidateServiceNameQuery : IRequest<(string?, string?)>
{
    private readonly ServiceName _serviceName;
    private readonly string _solutionPath;
    public ValidateServiceNameQuery(ServiceName serviceName, string solutionPath)
    {
        _serviceName = serviceName;
        _solutionPath = solutionPath;
    }

    public class Handler : IRequestHandler<ValidateServiceNameQuery, (string?, string?)>
    {
        private readonly ILogger<Handler> _logger;
        private const string ServiceProjectName = "Dfe.Spi.{0}.Function";

        public Handler(ILogger<Handler> logger)
        {
            _logger = logger;
        }

        public async Task<(string?, string?)> Handle(ValidateServiceNameQuery request,
            CancellationToken cancellationToken)
        {

            try
            {
                return await Task.Run(() =>
                {
                    var projectFolderName = string.Format(ServiceProjectName, request._serviceName);
                    var dirs = Directory.GetDirectories(request._solutionPath, $"*{projectFolderName}*", SearchOption.TopDirectoryOnly);
                    if (dirs.Length <= 0)
                        throw new Exception();
                    var filePath = System.IO.Directory.GetFiles(dirs[0], "*.csproj")
                        .FirstOrDefault(x => Path.GetExtension(x).ToLower() == ".csproj");
                    if (string.IsNullOrEmpty(filePath))
                        throw new Exception();
                    if (filePath.IndexOf(projectFolderName, StringComparison.Ordinal) <= -1) throw new Exception();
                    var filename = Path.GetFileName(filePath);

                    return (filename, filePath.Replace(filename, ""));
                }, cancellationToken);
            }
            catch (Exception e)
            {
                throw new SpiException(new List<string>()
                {
                    "Functions project file not found, please make sure you have selected correct service and provided a path to the solution folder!",
                }, null);
            }
        }
    }
}