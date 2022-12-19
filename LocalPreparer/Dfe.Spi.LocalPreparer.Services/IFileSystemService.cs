using Dfe.Spi.LocalPreparer.Domain.Enums;

namespace Dfe.Spi.LocalPreparer.Services;
public interface IFileSystemService
{
    (string?, string?) ValidateServiceName(ServiceName serviceName, string solutionPath);
    bool CopySettingFiles(string filename, string projectFolder);
}