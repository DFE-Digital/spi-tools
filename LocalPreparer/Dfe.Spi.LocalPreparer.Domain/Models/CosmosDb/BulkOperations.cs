using System.Diagnostics;

namespace Dfe.Spi.LocalPreparer.Domain.Models.CosmosDb;
public class BulkOperations<T>
{
    public readonly List<Task<OperationResponse<T>>> Tasks;

    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

    public BulkOperations(int operationCount)
    {
        this.Tasks = new List<Task<OperationResponse<T>>>(operationCount);
    }

    public async Task<BulkOperationResponse<T>> ExecuteAsync()
    {
        await Task.WhenAll(this.Tasks);
        this._stopwatch.Stop();
        return new BulkOperationResponse<T>()
        {
            TotalTimeTaken = this._stopwatch.Elapsed,
            TotalRequestUnitsConsumed = this.Tasks.Sum(task => task.Result.RequestUnitsConsumed),
            SuccessfulDocuments = this.Tasks.Count(task => task.Result.IsSuccessful),
            Failures = this.Tasks.Where(task => !task.Result.IsSuccessful).Select(task => (task.Result.Item, task.Result.CosmosException)).ToList()
        };
    }
}
