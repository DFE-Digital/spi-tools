namespace Dfe.Spi.HistoricalDataCapture.Infrastructure.GiasSoapApi
{
    public class SoapException : GiasSoapApiException
    {
        public string FaultCode { get; }

        public SoapException(string faultCode, string faultString)
            : base(faultString)
        {
            FaultCode = faultCode;
        }
    }
}