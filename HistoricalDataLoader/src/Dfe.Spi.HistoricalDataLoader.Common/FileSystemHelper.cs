using System;
using System.IO;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Dfe.Spi.HistoricalDataLoader.Common
{
    public static class FileSystemHelper
    {
        public static async Task<string> ReadFileAsStringAsync(string path)
        {
            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            using var reader = new StreamReader(stream);

            return await reader.ReadToEndAsync();
        }

        public static async Task<T> ReadFileAsAsync<T>(string path)
        {
            var json = await ReadFileAsStringAsync(path);
            return JsonConvert.DeserializeObject<T>(json);
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

        public static async Task WriteObjectToFileAsync<T>(string path, T obj, FileMode fileMode = FileMode.Create)
        {
            var json = JsonConvert.SerializeObject(obj);
            await WriteStringToFileAsync(path, json, fileMode);
        }
    }
}