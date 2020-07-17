using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.HistoricalDataPreparer.Domain.Gias;
using Dfe.Spi.HistoricalDataPreparer.Domain.Ukrlp;
using Serilog;

namespace Dfe.Spi.HistoricalDataPreparer.Application
{
    public interface IDayProcessor
    {
        Task ProcessDaysDataAsync(DateTime date, GiasDayData giasData, UkrlpDayData ukrlpDayData, CancellationToken cancellationToken);
    }

    public class DayProcessor : IDayProcessor
    {
        private readonly Dictionary<Type, PropertyInfo[]> _propertyCache;
        private readonly IPreparedGiasRepository _preparedGiasRepository;
        private readonly ILogger _logger;

        public DayProcessor(
            IPreparedGiasRepository preparedGiasRepository,
            ILogger logger)
        {
            _propertyCache = new Dictionary<Type, PropertyInfo[]>();
            _preparedGiasRepository = preparedGiasRepository;
            _logger = logger;
        }

        public async Task ProcessDaysDataAsync(DateTime date, GiasDayData giasData, UkrlpDayData ukrlpDayData, CancellationToken cancellationToken)
        {
            // GIAS
            var changedEstablishments = await ProcessEstablishmentsForDeltas(giasData.Establishments, giasData.GroupLinks, date, cancellationToken);
            var changedGroups = await ProcessGroupsForDeltas(giasData.Groups, date, cancellationToken);
            var changedLocalAuthorities = await ProcessLocalAuthoritiesForDeltas(giasData.LocalAuthorities, date, cancellationToken);
        }

        private async Task<Establishment[]> ProcessEstablishmentsForDeltas(Establishment[] establishments, GroupLink[] groupLinks, DateTime date, CancellationToken cancellationToken)
        {
            _logger.Information($"Processing establishments for {date:yyyy-MM-dd}");
            var changed = new List<Establishment>();
            
            for (var i = 0; i < establishments.Length && !cancellationToken.IsCancellationRequested; i++)
            {
                var establishment = establishments[i];
                
                // Add links
                var establishmentGroupLinks = groupLinks.Where(l => l.Urn == establishment.Urn).ToArray();
                var federationLink = establishmentGroupLinks.FirstOrDefault(l => l.GroupType == "Federation");
                var trustLink = establishmentGroupLinks.FirstOrDefault(l =>
                    l.GroupType == "Trust" || l.GroupType == "Single-academy trust" || l.GroupType == "Multi-academy trust");

                if (federationLink != null)
                {
                    establishment.Federations = new CodeNamePair
                    {
                        Code = federationLink.Uid.ToString(),
                    };
                }
                if (trustLink != null)
                {
                    establishment.Trusts = new CodeNamePair
                    {
                        Code = trustLink.Uid.ToString(),
                    };
                }
                
                // Check for change
                var previousVersion = await _preparedGiasRepository.GetEstablishmentAsync(establishment.Urn, date, cancellationToken);
                var hasChanged = previousVersion == null || HasChanged(previousVersion, establishment);

                if (hasChanged)
                {
                    _logger.Information($"{i} of {establishments.Length}: Establishment {{Urn}} has changed on {date:yyyy-MM-dd}; storing...", establishment.Urn);
                    await _preparedGiasRepository.StoreEstablishmentAsync(establishment, date, cancellationToken);
                    changed.Add(establishment);
                }
                else
                {
                    _logger.Debug($"{i} of {establishments.Length}: Establishment {{Urn}} has not changed on {date:yyyy-MM-dd}; skipping", establishment.Urn);
                }
            }

            return changed.ToArray();
        }
        private async Task<Group[]> ProcessGroupsForDeltas(Group[] groups, DateTime date, CancellationToken cancellationToken)
        {
            _logger.Information($"Processing groups for {date:yyyy-MM-dd}");
            var changed = new List<Group>();
            
            for (var i = 0; i < groups.Length && !cancellationToken.IsCancellationRequested; i++)
            {
                var group = groups[i];
                var previousVersion = await _preparedGiasRepository.GetGroupAsync(group.Uid, date, cancellationToken);
                var hasChanged = previousVersion == null || HasChanged(previousVersion, group);

                if (hasChanged)
                {
                    _logger.Information($"{i} of {groups.Length}: Group {{Uid}} has changed on {date:yyyy-MM-dd}; storing...", group.Uid);
                    await _preparedGiasRepository.StoreGroupAsync(group, date, cancellationToken);
                    changed.Add(group);
                }
                else
                {
                    _logger.Debug($"{i} of {groups.Length}: Group {{Uid}} has not changed on {date:yyyy-MM-dd}; skipping", group.Uid);
                }
            }

            return changed.ToArray();
        }
        private async Task<LocalAuthority[]> ProcessLocalAuthoritiesForDeltas(LocalAuthority[] localAuthorities, DateTime date, CancellationToken cancellationToken)
        {
            _logger.Information($"Processing local authorities for {date:yyyy-MM-dd}");
            var changed = new List<LocalAuthority>();
            
            for (var i = 0; i < localAuthorities.Length && !cancellationToken.IsCancellationRequested; i++)
            {
                var localAuthority = localAuthorities[i];
                var previousVersion = await _preparedGiasRepository.GetLocalAuthorityAsync(localAuthority.Code, date, cancellationToken);
                var hasChanged = previousVersion == null || HasChanged(previousVersion, localAuthority);

                if (hasChanged)
                {
                    _logger.Information($"{i} of {localAuthorities.Length}: Local authority {{LACode}} has changed on {date:yyyy-MM-dd}; storing...", localAuthority.Code);
                    await _preparedGiasRepository.StoreLocalAuthorityAsync(localAuthority, date, cancellationToken);
                    changed.Add(localAuthority);
                }
                else
                {
                    _logger.Debug($"{i} of {localAuthorities.Length}: Local authority {{LACode}} has not changed on {date:yyyy-MM-dd}; skipping", localAuthority.Code);
                }
            }

            return changed.ToArray();
        }

        
        private bool HasChanged<T>(T previous, T current)
        {
            return HasChanged(previous, current, typeof(T));
        }
        private bool HasChanged(object previous, object current, Type type)
        {
            var properties = _propertyCache.ContainsKey(type)
                ? _propertyCache[type]
                : null;
            if (properties == null)
            {
                properties = type.GetProperties();
                _propertyCache.Add(type, properties);
            }

            foreach (var property in properties)
            {
                try
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
                    if (property.PropertyType.IsClass && property.PropertyType.Namespace != "System")
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
                catch
                {
                    throw;
                }
            }

            return false;
        }
    }
}