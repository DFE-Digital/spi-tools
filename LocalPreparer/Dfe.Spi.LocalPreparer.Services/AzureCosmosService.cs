using Dfe.Spi.LocalPreparer.Azure.CosmosDb;
using Dfe.Spi.LocalPreparer.Azure.CosmosDb.Repositories;
using Dfe.Spi.LocalPreparer.Common;
using Dfe.Spi.LocalPreparer.Common.Presentation;
using Dfe.Spi.LocalPreparer.Common.Utils;
using Dfe.Spi.LocalPreparer.Domain.Enums;
using Dfe.Spi.LocalPreparer.Domain.Models;
using Dfe.Spi.LocalPreparer.Domain.Models.CosmosDb;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Net;

namespace Dfe.Spi.LocalPreparer.Services;

public class AzureCosmosService : IAzureCosmosService
{

    private readonly IOptions<SpiSettings> _configuration;
    private readonly ILogger<AzureStorageService> _logger;
    private readonly IContextManager _contextManager;
    private readonly ILocalCosmosRepository _localCosmosRepo;
    private readonly IRemoteCosmosRepository _remoteCosmosRepo;
    private readonly ICosmosContainerProvider _containerProvider;
    private const int BatchSize = 1000;
    private const int MaxRetryAttempts = 3;
    private int _retryAttempts;

    public AzureCosmosService(IOptions<SpiSettings> configuration, ILogger<AzureStorageService> logger, IContextManager contextManager, ILocalCosmosRepository localCosmosRepo, IRemoteCosmosRepository remoteCosmosRepo, ICosmosContainerProvider containerProvider)
    {
        _configuration = configuration;
        _logger = logger;
        _contextManager = contextManager;
        _localCosmosRepo = localCosmosRepo;
        _remoteCosmosRepo = remoteCosmosRepo;
        _containerProvider = containerProvider;
    }

    public async Task CopyCosmosDbData(ServiceName serviceName)
    {
        _retryAttempts = 0;
        var chosenOption = await Interactions.PromptAsync($"Please confirm if you would like your local CosmosDb container to be deleted and recreated! This will make the process significantly faster! " +
                            $"Choosing \"No\", means the application will use Upsert operation to import the data and it will take twice as long as using normal Create operation!)",
            async () => await _containerProvider.PrepareLocalDatabase(),
            false,
            ConsoleColor.Blue);

        var data = await _remoteCosmosRepo.GetAsync(x => true);
        await ImportDownloadedData(serviceName, data, chosenOption);
    }

    private async Task<IEnumerable<(CosmosRegisteredEntity, Exception)>> ImportDownloadedData(ServiceName serviceName, IEnumerable<CosmosRegisteredEntity> data, Interactions.PromptOptions chosenOption)
    {
        var timer = new Stopwatch();
        timer.Start();

        var operationResponse =
            new Dictionary<int, BulkOperationResponse<CosmosRegisteredEntity>>(BatchSize);

        var chunks = (data.Split(BatchSize)).ToArray();

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
            Thread.Sleep(100);
        }

        var errors =
            operationResponse.Values.SelectMany(x => x.Failures).ToArray();

        if (errors.Any())
        {
            return await Retry(errors, serviceName, chosenOption);
        }
        else
        {
            _logger.LogInformation($"**** Operation completed! ****");
        }

        timer.Stop();
        _logger.LogInformation($"**** Total time taken: {timer.Elapsed} ****");

        Interactions.Exit("Press any key to exist the application...");
        return errors;
    }

    private async Task PrintErrors(IEnumerable<(CosmosRegisteredEntity, Exception)> errors)
    {
        await (errors.Select(x => $"{x.Item1.Id} - {x.Item2.Message}"))
            .CreateTextFile(Path.Combine(Directory.GetCurrentDirectory(), "FailedItems.txt"));
    }

    private async Task<IEnumerable<(CosmosRegisteredEntity, Exception)>> Retry(IEnumerable<(CosmosRegisteredEntity, Exception)> errors, ServiceName serviceName, Interactions.PromptOptions chosenOption)
    {
        var valueTuples = errors as (CosmosRegisteredEntity, Exception)[] ?? errors.ToArray();
        _logger.LogInformation($"**** Operation completed with {valueTuples.Count()} failed items ****");

        if (_retryAttempts < MaxRetryAttempts)
        {
            _logger.LogInformation($"Retrying failed items...");
            var eligibleFailedItems = valueTuples.Where(x =>
                    x.Item2 is CosmosException { StatusCode: HttpStatusCode.RequestTimeout } or CosmosException { StatusCode: HttpStatusCode.ServiceUnavailable } or CosmosException { StatusCode: HttpStatusCode.TooManyRequests })
                .Select(x => x.Item1)
                .ToList();

            if (eligibleFailedItems.Any())
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