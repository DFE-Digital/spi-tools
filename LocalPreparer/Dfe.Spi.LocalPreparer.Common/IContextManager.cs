
using Dfe.Spi.LocalPreparer.Domain.Enums;
using Dfe.Spi.LocalPreparer.Domain.Models;

namespace Dfe.Spi.LocalPreparer.Common;

public interface IContextManager
{
    Context Context { get; }
    void SetContext(Context context);
    void SetActiveService(ServiceName service);
}