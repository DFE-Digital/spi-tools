using Dfe.Spi.HistoricalDataLoader.GiasLoaderConsoleApp.Models;
using Newtonsoft.Json;
using Serilog;

namespace Dfe.Spi.HistoricalDataLoader.GiasLoaderConsoleApp.Loaders
{
    internal class GroupLoader : LoaderBase<Group, long, GroupEntity>
    {
        public GroupLoader(string dataDirectory, string storageConnectionString, ILogger logger)
            : base(dataDirectory, storageConnectionString, "groups", "groups", "group", logger)
        {
        }

        protected override GroupEntity ConvertModelToEntity(Group model)
        {
            return new GroupEntity
            {
                Group = JsonConvert.SerializeObject(model),
            };
        }
    }

    internal class GroupEntity : LoaderTableEntityBase
    {
        public string Group
        {
            get;
            set;
        }
    }
}