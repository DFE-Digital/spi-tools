using System;

namespace Dfe.Spi.HistoricalDataCapture.Infrastructure.UkrlpSoapApi
{
    public class UkrlpSoapApiException : Exception
    {
        public UkrlpSoapApiException(string message)
            : base(message)
        {
        }

        public UkrlpSoapApiException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}