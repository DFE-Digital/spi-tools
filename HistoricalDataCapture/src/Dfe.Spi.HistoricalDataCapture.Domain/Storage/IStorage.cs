using System.Threading;
using System.Threading.Tasks;

namespace Dfe.Spi.HistoricalDataCapture.Domain.Storage
{
    public interface IStorage
    {
        Task StoreAsync(string folder, string fileName, byte[] data, CancellationToken cancellationToken);
    }
}