using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Dfe.Spi.HistoricalDataPreparer.Application.ChangeProcessing;
using Dfe.Spi.HistoricalDataPreparer.Domain.Gias;
using Dfe.Spi.HistoricalDataPreparer.Domain.Registry;
using Dfe.Spi.HistoricalDataPreparer.Domain.Statistics;
using Dfe.Spi.HistoricalDataPreparer.Domain.Translation;
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
        private GiasChangeProcessor _giasChangeProcessor;
        private UkrlpChangeProcessor _ukrlpChangeProcessor;
        private RegistryChangeProcessor _registryChangeProcessor;
        private readonly IStatisticsRepository _statisticsRepository;
        private readonly ILogger _logger;

        public DayProcessor(
            IPreparedGiasRepository preparedGiasRepository,
            IPreparedUkrlpRepository preparedUkrlpRepository,
            IPreparedRegistryRepository preparedRegistryRepository,
            ITranslation translation,
            IStatisticsRepository statisticsRepository,
            ILogger logger)
        {
            _giasChangeProcessor = new GiasChangeProcessor(preparedGiasRepository, logger);
            _ukrlpChangeProcessor = new UkrlpChangeProcessor(preparedUkrlpRepository, logger);
            _registryChangeProcessor = new RegistryChangeProcessor(
                preparedRegistryRepository,
                preparedGiasRepository,
                preparedUkrlpRepository,
                translation, 
                logger);
            _statisticsRepository = statisticsRepository;
            _logger = logger;
        }

        public async Task ProcessDaysDataAsync(DateTime date, GiasDayData giasData, UkrlpDayData ukrlpDayData, CancellationToken cancellationToken)
        {
            var startTime = DateTime.Now;
            
            // GIAS
            var changedEstablishments = await _giasChangeProcessor.ProcessEstablishmentsForDeltasAsync(giasData.Establishments, giasData.GroupLinks, date, cancellationToken);
            var changedGroups = await _giasChangeProcessor.ProcessGroupsForDeltasAsync(giasData.Groups, date, cancellationToken);
            var changedLocalAuthorities = await _giasChangeProcessor.ProcessLocalAuthoritiesForDeltasAsync(giasData.LocalAuthorities, date, cancellationToken);

            // UKRLP
            var changedProviders = await _ukrlpChangeProcessor.ProcessProvidersForDeltasAsync(ukrlpDayData.Providers, date, cancellationToken);
            
            // Registry
            var changedRegistryEntries = await _registryChangeProcessor.ProcessChangesForDeltasAsync(
                date,
                changedEstablishments,
                changedGroups,
                changedLocalAuthorities,
                changedProviders,
                cancellationToken);
            
            // Save stats
            var duration = DateTime.Now - startTime;
            await _statisticsRepository.StoreDateStatisticsAsync(new DateStatistics
            {
                Date = date,
                Duration = duration,
                EstablishmentsChanged = changedEstablishments.Length,
                GroupsChanged = changedGroups.Length,
                LocalAuthoritiesChanged = changedLocalAuthorities.Length,
                ProvidersChanged = changedProviders.Length,
                RegistryEntriesChanged = changedRegistryEntries.Length,
            }, cancellationToken);
        }
    }
}