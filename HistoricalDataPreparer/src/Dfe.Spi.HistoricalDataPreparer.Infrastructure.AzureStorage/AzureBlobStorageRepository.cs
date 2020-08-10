using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Dfe.Spi.HistoricalDataPreparer.Infrastructure.AzureStorage
{
    public class AzureBlobStorageRepository
    {
        private readonly BlobServiceClient _serviceClient;
        private readonly BlobContainerClient _containerClient;

        public AzureBlobStorageRepository(string connectionString, string containerName)
        {
            _serviceClient = new BlobServiceClient(connectionString);
            _containerClient = _serviceClient.GetBlobContainerClient(containerName);
        }

        protected async Task<BlobItem[]> ListBlobsAsync(string prefix, CancellationToken cancellationToken)
        {
            var blobs = new List<BlobItem>();
            
            await foreach (var blobItem in _containerClient.GetBlobsAsync(prefix: prefix, cancellationToken: cancellationToken))
            {
                blobs.Add(blobItem);
            }

            return blobs.ToArray();
        }

        protected async Task<Stream> DownloadAsync(BlobItem blob, CancellationToken cancellationToken)
        {
            var blobClient = _containerClient.GetBlobClient(blob.Name);
            var downloadStream = new MemoryStream();

            try
            {
                await blobClient.DownloadToAsync(downloadStream, cancellationToken);
            }
            catch
            {
                await downloadStream.DisposeAsync();
                throw;
            }

            downloadStream.Position = 0;
            return downloadStream;
        }
    }
}