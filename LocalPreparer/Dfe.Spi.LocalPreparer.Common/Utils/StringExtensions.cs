using System.Text;
using Dfe.Spi.LocalPreparer.Common.Configurations;

namespace Dfe.Spi.LocalPreparer.Common.Utils;
public static class StringExtensions
{

    public static string? GetAppVersion()
    {
        return $"{System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version}";
    }
    public static bool EndsWith(this StringBuilder sb, string test)
    {
        return EndsWith(sb, test, StringComparison.CurrentCulture);
    }

    public static bool EndsWith(this StringBuilder sb, string test,
        StringComparison comparison)
    {
        return sb.Length >= test.Length &&
               sb.ToString().EndsWith(test);
    }

    public static string ToResourceGroup(this string storageAccountName)
    {
        var length = Constants.AzureEnvironmentId.Length;
        return storageAccountName.Insert(length, "-");
    }

}