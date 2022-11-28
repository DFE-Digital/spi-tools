using Dfe.Spi.LocalPreparer.Common.Enums;

namespace Dfe.Spi.LocalPreparer.Services
{
    public interface IFileSystemService
    {
        (string?, string?) ValidateServiceName(ServiceName serviceName, string solutionPath);
        bool CopySettingFiles(string filename, string projectFolder);
    }
}