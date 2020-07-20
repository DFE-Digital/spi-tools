using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Dfe.Spi.HistoricalDataPreparer.Application.ChangeProcessing
{
    public static class ReflectionHelper
    {
        private static readonly Dictionary<Type, PropertyInfo[]> _propertyCache = new Dictionary<Type, PropertyInfo[]>();

        public static PropertyInfo[] GetPropertiesOf(Type type)
        {
            if (_propertyCache.ContainsKey(type))
            {
                return _propertyCache[type];
            }
            
            var properties = type.GetProperties()
                .Where(p => !p.Name.Equals("LastUpdated", StringComparison.InvariantCultureIgnoreCase) &&
                            !p.Name.Equals("LastChangedDate", StringComparison.InvariantCultureIgnoreCase) &&
                            !p.Name.Equals("ProviderVerificationDate", StringComparison.InvariantCultureIgnoreCase) &&
                            !p.Name.Equals("ExpiryDate", StringComparison.InvariantCultureIgnoreCase))
                .ToArray();
            _propertyCache.Add(type, properties);
            return properties;
        }
    }
}