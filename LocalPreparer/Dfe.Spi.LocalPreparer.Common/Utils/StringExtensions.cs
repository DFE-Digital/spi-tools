using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;


namespace Dfe.Spi.LocalPreparer.Common.Utils
{
    public static class StringExtensions
    {

        public static string? GetAppVersion()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            return fileVersionInfo.ProductVersion;
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