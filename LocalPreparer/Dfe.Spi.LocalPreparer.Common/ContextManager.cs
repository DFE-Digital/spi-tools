
using Dfe.Spi.LocalPreparer.Domain.Enums;
using Dfe.Spi.LocalPreparer.Domain.Models;

namespace Dfe.Spi.LocalPreparer.Common;

public class ContextManager : IContextManager
{
    public Context Context { get; private set; }

    public void SetContext(Context context)
    {
        Context = context;
    }

    public void SetActiveService(ServiceName service)
    {
        Context.ActiveService = service;
    }
}