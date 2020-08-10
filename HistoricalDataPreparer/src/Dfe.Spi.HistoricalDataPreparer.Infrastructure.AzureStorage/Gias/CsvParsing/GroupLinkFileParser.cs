using System.IO;
using CsvHelper.Configuration;
using Dfe.Spi.HistoricalDataPreparer.Domain.Gias;

namespace Dfe.Spi.HistoricalDataPreparer.Infrastructure.AzureStorage.Gias.CsvParsing
{
    public class GroupLinkFileParser : CsvFileParser<GroupLink>
    {
        private class GroupLinkCsvMapping : ClassMap<GroupLink>
        {
            public GroupLinkCsvMapping()
            {
                var dateTimeConverter = new DateTimeConverter();

                Map(x => x.Uid).Name("Linked UID");
                Map(x => x.Urn).Name("URN");
                Map(x => x.GroupType).Name("Group Type");
            }
        }

        public GroupLinkFileParser(StreamReader reader)
            : base(reader, new GroupLinkCsvMapping())
        {
        }
    }
}