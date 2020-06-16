using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dfe.Spi.HistoricalDataCapture.Domain.UkrlpClient
{
    public interface IUkrlpClient
    {
        Task<byte[]> GetChangesSinceAsync(DateTime sinceTime, CancellationToken cancellationToken);
    }
}