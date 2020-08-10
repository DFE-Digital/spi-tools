using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Dfe.Spi.HistoricalDataPreparer.Application.ChangeProcessing
{
    public abstract class ChangeProcessor
    {
        protected bool HasChanged<T>(T previous, T current)
        {
            return HasChanged(previous, current, typeof(T));
        }

        protected bool HasChanged(object previous, object current, Type type)
        {
            var properties = ReflectionHelper.GetPropertiesOf(type);

            foreach (var property in properties)
            {
                var previousValue = property.GetValue(previous);
                var currentValue = property.GetValue(current);

                // If they both null then property has not changed
                if (previousValue == null && currentValue == null)
                {
                    continue;
                }

                // As they are both not null, if one of them is null then they are different
                if (previousValue == null || currentValue == null)
                {
                    return true;
                }

                bool propertyHasChanged;
                if (property.PropertyType.IsArray)
                {
                    propertyHasChanged = HasArrayChanged((Array) previousValue, (Array) currentValue, property.PropertyType.GetElementType());
                }
                else if (property.PropertyType.IsClass && property.PropertyType.Namespace != "System")
                {
                    propertyHasChanged = HasChanged(previousValue, currentValue, property.PropertyType);
                }
                else
                {
                    propertyHasChanged = previousValue == null || !previousValue.Equals(currentValue);
                }

                if (propertyHasChanged)
                {
                    return true;
                }
            }

            return false;
        }

        protected bool HasArrayChanged(Array previous, Array current, Type elementType)
        {
            if (previous.Length != current.Length)
            {
                return true;
            }

            foreach (var currentItem in current)
            {
                var hasMatchingItem = false;
                foreach (var previousItem in previous)
                {
                    if (!HasChanged(previousItem, currentItem, elementType))
                    {
                        hasMatchingItem = true;
                        break;
                    }
                }

                if (!hasMatchingItem)
                {
                    return true;
                }
            }

            return false;
        }
    }
}