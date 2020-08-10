using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.HistoricalDataPreparer.Domain.Ukrlp;
using Serilog;

namespace Dfe.Spi.HistoricalDataPreparer.Application.ChangeProcessing
{
    public class UkrlpChangeProcessor : ChangeProcessor
    {
        private readonly IPreparedUkrlpRepository _preparedUkrlpRepository;
        private readonly ILogger _logger;

        public UkrlpChangeProcessor(
            IPreparedUkrlpRepository preparedUkrlpRepository,
            ILogger logger)
        {
            _preparedUkrlpRepository = preparedUkrlpRepository;
            _logger = logger;
        }
        
        public async Task<Provider[]> ProcessProvidersForDeltasAsync(Provider[] providers, DateTime date, CancellationToken cancellationToken)
        {
            _logger.Information($"Processing providers for {date:yyyy-MM-dd}");
            var changed = new List<Provider>();

            for (var i = 0; i < providers.Length && !cancellationToken.IsCancellationRequested; i++)
            {
                var provider = providers[i];
                var previousVersion = await _preparedUkrlpRepository.GetProviderAsync(provider.UnitedKingdomProviderReferenceNumber, date, cancellationToken);
                var hasChanged = previousVersion == null || HasChanged(previousVersion, provider);

                if (hasChanged)
                {
                    _logger.Information($"{i} of {providers.Length}: Provider {{UKPRN}} has changed on {date:yyyy-MM-dd}; storing...",
                        provider.UnitedKingdomProviderReferenceNumber);
                    await _preparedUkrlpRepository.StoreProviderAsync(provider, date, cancellationToken);
                    changed.Add(provider);
                }
                else
                {
                    _logger.Debug($"{i} of {providers.Length}: Provider {{UKPRN}} has not changed on {date:yyyy-MM-dd}; skipping",
                        provider.UnitedKingdomProviderReferenceNumber);
                }
            }

            return changed.ToArray();
        }
    }
}