using System.IO;
using CsvHelper.Configuration;
using Dfe.Spi.HistoricalDataPreparer.Domain.Gias;

namespace Dfe.Spi.HistoricalDataPreparer.Infrastructure.AzureStorage.Gias.CsvParsing
{
    public class GroupFileParser : CsvFileParser<Group>
    {
        private class GroupCsvMapping : ClassMap<Group>
        {
            public GroupCsvMapping()
            {
                var dateTimeConverter = new DateTimeConverter();

                Map(x => x.Uid).Name("UID");
                Map(x => x.GroupName).Name("Group Name");
                Map(x => x.CompaniesHouseNumber).Name("Companies House Number");
                Map(x => x.Ukprn).Name("UKPRN");
                Map(x => x.GroupType).Name("Group Type");
                Map(x => x.ClosedDate).Name("Closed Date").TypeConverter(dateTimeConverter);
                Map(x => x.Status).Name("Status");
                Map(x => x.GroupContactStreet).Name("Group Contact Street");
                Map(x => x.GroupContactLocality).Name("Group Contact Locality");
                Map(x => x.GroupContactAddress3).Name("Group Contact Address 3");
                Map(x => x.GroupContactTown).Name("Group Contact Town");
                Map(x => x.GroupContactCounty).Name("Group Contact County");
                Map(x => x.GroupContactPostcode).Name("Group Contact Postcode");
                Map(x => x.HeadOfGroupTitle).Name("Head of Group Title");
                Map(x => x.HeadOfGroupFirstName).Name("Head of Group First Name");
                Map(x => x.HeadOfGroupLastName).Name("Head of Group Last Name");
                Map(x => x.GroupStreet).Name("Group Street");
                Map(x => x.GroupLocality).Name("Group Locality");
                Map(x => x.GroupAddress3).Name("Group Address 3");
                Map(x => x.GroupTown).Name("Group Town");
                Map(x => x.GroupCounty).Name("Group County");
                Map(x => x.GroupPostcode).Name("Group Postcode");
            }
        }

        public GroupFileParser(StreamReader reader)
            : base(reader, new GroupCsvMapping())
        {
        }
    }
}