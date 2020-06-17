using System.Threading;
using System.Threading.Tasks;

namespace Dfe.Spi.HistoricalDataCapture.Domain.GiasClient
{
    public interface IGiasClient
    {
        Task<byte[]> DownloadExtractAsync(int extractId, CancellationToken cancellationToken);
    }
}