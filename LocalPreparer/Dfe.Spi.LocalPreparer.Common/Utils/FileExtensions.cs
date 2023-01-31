namespace Dfe.Spi.LocalPreparer.Common.Utils
{
    public static class FileExtensions
    {

        public static async Task CreateTextFile(this IEnumerable<string> items, string fileName)
        {
            await File.WriteAllLinesAsync(fileName, items);
        }

    }
}
