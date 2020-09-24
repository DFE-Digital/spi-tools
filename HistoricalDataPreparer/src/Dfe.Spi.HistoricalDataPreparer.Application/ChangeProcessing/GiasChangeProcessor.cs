using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.HistoricalDataPreparer.Domain.Gias;
using Serilog;

namespace Dfe.Spi.HistoricalDataPreparer.Application.ChangeProcessing
{
    public class GiasChangeProcessor : ChangeProcessor
    {
        private readonly IPreparedGiasRepository _preparedGiasRepository;
        private readonly ILogger _logger;

        public GiasChangeProcessor(
            IPreparedGiasRepository preparedGiasRepository,
            ILogger logger)
        {
            _preparedGiasRepository = preparedGiasRepository;
            _logger = logger;
        }
        
        public async Task<Establishment[]> ProcessEstablishmentsForDeltasAsync(Establishment[] establishments, GroupLink[] groupLinks, DateTime date,
            CancellationToken cancellationToken)
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
                    _logger.Information($"{i} of {establishments.Length}: Establishment {{Urn}} has changed on {date:yyyy-MM-dd}; storing...",
                        establishment.Urn);
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

        public async Task<Group[]> ProcessGroupsForDeltasAsync(Group[] groups, DateTime date, CancellationToken cancellationToken)
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

        public async Task<LocalAuthority[]> ProcessLocalAuthoritiesForDeltasAsync(LocalAuthority[] localAuthorities, DateTime date,
            CancellationToken cancellationToken)
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
                    _logger.Information($"{i} of {localAuthorities.Length}: Local authority {{LACode}} has changed on {date:yyyy-MM-dd}; storing...",
                        localAuthority.Code);
                    await _preparedGiasRepository.StoreLocalAuthorityAsync(localAuthority, date, cancellationToken);
                    changed.Add(localAuthority);
                }
                else
                {
                    _logger.Debug($"{i} of {localAuthorities.Length}: Local authority {{LACode}} has not changed on {date:yyyy-MM-dd}; skipping",
                        localAuthority.Code);
                }
            }

            return changed.ToArray();
        }
    }
}