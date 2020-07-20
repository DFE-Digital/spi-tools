using System.Threading;
using System.Threading.Tasks;

namespace Dfe.Spi.HistoricalDataPreparer.Domain.Translation
{
    public interface ITranslation
    {
        Task<string> TranslateEnumValueAsync(string enumName, string sourceSystem, string sourceValue, CancellationToken cancellationToken);
    }
}