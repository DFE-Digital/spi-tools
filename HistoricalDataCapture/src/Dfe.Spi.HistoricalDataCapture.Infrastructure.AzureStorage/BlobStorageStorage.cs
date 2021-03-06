﻿using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Dfe.Spi.HistoricalDataCapture.Domain.Configuration;
using Dfe.Spi.HistoricalDataCapture.Domain.Storage;

namespace Dfe.Spi.HistoricalDataCapture.Infrastructure.AzureStorage
{
    public class BlobStorageStorage : IStorage
    {
        private readonly StorageConfiguration _configuration;
        private BlobServiceClient _client;

        public BlobStorageStorage(StorageConfiguration configuration)
        {
            _configuration = configuration;
            _client = new BlobServiceClient(_configuration.BlobConnectionString);
        }

        public async Task StoreAsync(string folder, string fileName, byte[] data, CancellationToken cancellationToken)
        {
            var container = await GetOrCreateBlobContainerAsync(folder, cancellationToken);
            var blob = container.GetBlobClient(fileName);

            using (var stream = new MemoryStream(data))
            {
                await blob.UploadAsync(stream, true, cancellationToken);
            }
        }

        public async Task<byte[]> ReadAsync(string folder, string fileName, CancellationToken cancellationToken)
        {
            var container = await GetOrCreateBlobContainerAsync(folder, cancellationToken);
            var blob = container.GetBlobClient(fileName);

            try
            {
                var downloadResult = await blob.DownloadAsync(cancellationToken);
                var buffer = new byte[1024];
                await downloadResult.Value.Content.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                return buffer;
            }
            catch (RequestFailedException ex)
            {
                if(ex.ErrorCode=="BlobNotFound")
                {
                    return null;
                }
                throw;
            }
        }

        private async Task<BlobContainerClient> GetOrCreateBlobContainerAsync(string containerName, CancellationToken cancellationToken)
        {
            var container = _client.GetBlobContainerClient(containerName);
            await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
            return container;
        }
    }
}