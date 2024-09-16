﻿namespace Dfe.Spi.LocalPreparer.Domain.Models.CosmosDb;

public class BulkOperationResponse<T>
{
    public TimeSpan TotalTimeTaken { get; set; }
    public int SuccessfulDocuments { get; set; } = 0;
    public double TotalRequestUnitsConsumed { get; set; } = 0;

    public IReadOnlyList<(T, Exception)> Failures { get; set; }
}