using System;

namespace Dfe.Spi.HistoricalDataPreparer.Infrastructure.SpiApi
{
    public class OAuthException : Exception
    {
        public OAuthException(string message)
            : base(message)
        {
        }
    }
}