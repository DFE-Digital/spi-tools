using Dfe.Spi.LocalPreparer.Azure.CosmosDb;
using Dfe.Spi.LocalPreparer.Azure.CosmosDb.Repositories;
using Dfe.Spi.LocalPreparer.Common.Presentation;
using Dfe.Spi.LocalPreparer.Common.Utils;
using Dfe.Spi.LocalPreparer.Domain.Enums;
using Dfe.Spi.LocalPreparer.Domain.Models;
using Dfe.Spi.LocalPreparer.Domain.Models.CosmosDb;
using MediatR;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Net;

namespace Dfe.Spi.LocalPreparer.Services.Mediator.Cosmos.Command;

public class CopyCosmosDataCommand : IRequest<bool>
{
    private readonly ServiceName _serviceName;

    public CopyCosmosDataCommand(ServiceName serviceName)
    {
        _serviceName = serviceName;
    }

    public class Handler : IRequestHandler<CopyCosmosDataCommand, bool>
    {
        private readonly IOptions<SpiSettings> _configuration;
        private readonly ILogger<Handler> _logger;
        private readonly ILocalCosmosRepository _localCosmosRepo;
        private readonly IRemoteCosmosRepository _remoteCosmosRepo;
        private readonly ICosmosContainerProvider _containerProvider;
        private const int BatchSize = 500;
        const string FaileditemsTxt = "FailedItems.txt";
        private int _retryAttempts;
        private readonly Stopwatch _totalTime = new();

        public Handler(IOptions<SpiSettings> configuration, ILogger<Handler> logger, ILocalCosmosRepository localCosmosRepo, IRemoteCosmosRepository remoteCosmosRepo, ICosmosContainerProvider containerProvider)
        {
            _configuration = configuration;
            _logger = logger;
            _localCosmosRepo = localCosmosRepo;
            _remoteCosmosRepo = remoteCosmosRepo;
            _containerProvider = containerProvider;
        }

        public async Task<bool> Handle(CopyCosmosDataCommand request, CancellationToken cancellationToken)
        {
            _retryAttempts = 0;
            try
            {

                var chosenOption = await Interactions.PromptAsync($"Please confirm if you would like your local CosmosDb container to be deleted and recreated! " +
                                                                  $"Choosing \"No\", means the application will use Upsert operation to import the data, updating existing items and inserting new ones!",
                async () => await _containerProvider.PrepareLocalDatabase(),
                false,
                ConsoleColor.Blue);

                var data = await _remoteCosmosRepo.GetAsync(x => true);
                await ImportDownloadedData(request._serviceName, data, chosenOption);

                return true;
            }
            catch (Exception e)
            {
                if (e is SpiException spiException)
                {
                    throw new SpiException(spiException.Errors, e);
                }
                throw new SpiException(new List<string>()
                {
                    "Failed to copy cosmosDb data!",
                }, e);
            }
        }

        private async Task<IEnumerable<(CosmosRegisteredEntity, Exception)>> ImportDownloadedData(ServiceName serviceName, ReadOnlyMemory<CosmosRegisteredEntity> data, Interactions.PromptOptions chosenOption)
        {
            _totalTime.Start();

            var operationResponse =
                new Dictionary<int, BulkOperationResponse<CosmosRegisteredEntity>>(BatchSize);

            var chunks = data.SplitIntoChunks(BatchSize).ToArray();

            for (var i = 0; i < chunks.Length; i++)
            {
                _logger.LogInformation($"Processing batch #{i + 1}/{chunks.Length}");
                if (chosenOption is Interactions.PromptOptions.No)
                {
                    operationResponse.Add(i, await _localCosmosRepo.BulkUpsertAsync(chunks[i]));
                }
                else
                {
                    operationResponse.Add(i, await _localCosmosRepo.BulkCreateAsync(chunks[i]));
                }
                await Task.Delay(250);
            }

            var errors =
                operationResponse.Values.SelectMany(x => x.Failures).ToArray();

            if (errors.Any())
                return await Retry(errors, serviceName, chosenOption);
            _logger.LogInformation($"**** Operation completed! ****");


            _totalTime.Stop();
            _logger.LogInformation($"**** Total time taken: {_totalTime.Elapsed} ****");

            return errors;
        }
        private async Task PrintErrors(IEnumerable<(CosmosRegisteredEntity, Exception)> errors)
        {
            await errors.Select(x => $"{x.Item1.Id} - {x.Item2.Message}")
                .CreateTextFile(Path.Combine(Directory.GetCurrentDirectory(), FaileditemsTxt));
        }

        private async Task<IEnumerable<(CosmosRegisteredEntity, Exception)>> Retry(IEnumerable<(CosmosRegisteredEntity, Exception)> errors, ServiceName serviceName, Interactions.PromptOptions chosenOption)
        {
            var maxRetryAttempts = _configuration.Value.Services?.GetValueOrDefault(serviceName)?.CosmosMaxRetryAttempts ?? 10;
            var valueTuples = errors as (CosmosRegisteredEntity, Exception)[] ?? errors.ToArray();
            _logger.LogInformation($"**** Operation completed with {valueTuples.Count()} failed items ****");

            if (_retryAttempts < maxRetryAttempts)
            {
                _logger.LogInformation($"Retrying failed items...");
                var eligibleFailedItems = valueTuples.Where(x =>
                        x.Item2 is CosmosException { StatusCode: HttpStatusCode.RequestTimeout } or CosmosException { StatusCode: HttpStatusCode.ServiceUnavailable } or CosmosException { StatusCode: HttpStatusCode.TooManyRequests })
                    .Select(x => x.Item1)
                    .ToArray().AsMemory();

                if (eligibleFailedItems.Length > 0)
                {
                    _retryAttempts++;
                    _logger.LogInformation($"Retry attempt {_retryAttempts}");
                    return await ImportDownloadedData(serviceName, eligibleFailedItems, chosenOption);
                }
                _logger.LogInformation($"No eligible failed item found to retry, please check the log file to view list of failed entities!");
                await PrintErrors(valueTuples);
            }
            else
            {
                _logger.LogInformation($"Reached maximum retry attempts! Please check the log file to view a list of failed entities!");
                await PrintErrors(valueTuples);
            }

            return valueTuples;
        }
    }
}