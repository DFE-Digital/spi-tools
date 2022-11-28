namespace Dfe.Spi.LocalPreparer.Common.Utils
{
    public static class ObjectExtensions
    {
        public static object GetPropertyValue(this object obj, string propertyName)
        {
            var props = obj.GetType().GetProperties();
            var pvalue = props.Single(pi => pi.Name == propertyName)
               .GetValue(obj, null);
            return pvalue;
        }

        public static IEnumerable<KeyValuePair<string, string>> PropertiesOfType(this object obj, string[] properties)
        {
            return from p in obj.GetType().GetProperties()
                   where properties.Contains(p.Name)
                   select new KeyValuePair<string, string>(p.Name, (string)p.GetValue(obj));
        }

    }
}
