using System.Diagnostics;
using System.Reflection;
using System.Text;


namespace Dfe.Spi.LocalPreparer.Common.Utils
{
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
            if (sb.Length < test.Length)
                return false;

            //string end = sb.ToString(sb.Length - test.Length, test.Length).;
            return sb.ToString().EndsWith(test);
        }

    }
}