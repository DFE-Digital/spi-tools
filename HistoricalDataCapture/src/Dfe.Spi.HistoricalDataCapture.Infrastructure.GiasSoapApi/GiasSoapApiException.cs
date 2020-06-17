using System;

namespace Dfe.Spi.HistoricalDataCapture.Infrastructure.GiasSoapApi
{
    public class GiasSoapApiException : Exception
    {
        public GiasSoapApiException(string message)
            : base(message)
        {
        }
        public GiasSoapApiException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}