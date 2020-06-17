using System.Linq;
using System.Xml.Linq;

namespace Dfe.Spi.HistoricalDataCapture.Infrastructure.GiasSoapApi
{
    internal static class XElementExtensions
    {
        internal static XElement GetElementByLocalName(this XElement containerElement, string localName)
        {
            return containerElement.Elements().SingleOrDefault(e => e.Name.LocalName == localName);
        }

        internal static string GetValueFromChildElement(this XElement containerElement, string localName)
        {
            XElement element = containerElement.GetElementByLocalName(localName);

            if (element == null || string.IsNullOrEmpty(element.Value))
            {
                return null;
            }

            return element.Value;
        }
    }
}