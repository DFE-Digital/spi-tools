using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;

namespace Dfe.Spi.LocalPreparer.Common.Utils
{
    public static class AzCopyLauncher
    {

        public static async Task<bool> RunAsync<T>(string arguments, ILogger<T> _logger)
        {
            _logger.LogWarning($"Start of AzCopy logs...{Environment.NewLine}");
            // making sure no other process is running
            var ps = Process.GetProcessesByName("azcopy");
            foreach (Process p in ps)
                p.Kill();
            var tempJournalPath = Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "AzCopy", "Temp", $"{Guid.NewGuid()}"));
            var succeeded = await StartAzCopyAsync($"{arguments} /Z:\"{tempJournalPath}\"", _logger);

            _logger.LogWarning($"End of AzCopy logs...{Environment.NewLine}");

            return succeeded;

        }

        private static async Task<bool> StartAzCopyAsync<T>(string arguments, ILogger<T> _logger, CancellationToken cancellationToken = default)
        {
            await Task.Run(() =>
            {

                var failed = false;
                var azCopyPath = Path.Combine(Directory.GetCurrentDirectory(), "AzCopy", "AzCopy.exe");
                var proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = azCopyPath,
                        Arguments = arguments,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardInput = true,
                        CreateNoWindow = true,
                        RedirectStandardError = true
                    }
                };
                proc.Start();

                var outStream = proc.StandardOutput;
                var input = new StringBuilder();

                while (!outStream.EndOfStream)
                {
                    var inputChar = (char)outStream.Read();
                    input.Append(inputChar);

                    if (input.EndsWith(Environment.NewLine))
                    {
                        var line = input.ToString();
                        input = new StringBuilder();
                        _logger.LogInformation(PrepareAzCopyLog(line));
                    }
                    else if (input.EndsWith("(Yes/No/All)"))
                    {
                        var line = input.ToString();
                        input = new StringBuilder();
                        _logger.LogInformation(line);
                        proc.StandardInput.Write("Y");
                    }
                }

                proc.BeginErrorReadLine();
                proc.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        throw new Exception(e.Data);
                    }
                };

                proc.WaitForExit();
                return !failed;
            });
            return true;
        }

        private static string? PrepareAzCopyLog(string value)
        {
            return string.IsNullOrEmpty(value) ? null : value[..];
        }
    }
}
