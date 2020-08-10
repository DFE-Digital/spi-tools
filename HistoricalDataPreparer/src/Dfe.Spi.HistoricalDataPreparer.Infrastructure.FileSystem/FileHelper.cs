using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Dfe.Spi.HistoricalDataPreparer.Infrastructure.FileSystem
{
    public static class FileHelper
    {
        public static async Task<string> ReadFileAsStringAsync(string path)
        {
            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            using var reader = new StreamReader(stream);

            return await reader.ReadToEndAsync();
        }

        public static async Task WriteStringToFileAsync(string path, string content, FileMode fileMode = FileMode.Create)
        {
            var directory = new DirectoryInfo(Path.GetDirectoryName(path));
            if (!directory.Exists)
            {
                directory.Create();
            }
            
            await using var stream = new FileStream(path, fileMode, FileAccess.Write);
            await using var writer = new StreamWriter(stream);

            await writer.WriteAsync(content);
            await writer.FlushAsync();
            writer.Close();
        }
    }
}