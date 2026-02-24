using EtlFunction.Models;

namespace EtlFunction.Contracts;

/// <summary>
/// Data access abstraction for supplier ETL pipeline.
/// </summary>
public interface ISupplierRepository
{
    /// <summary>
    /// Starts ETL run persistence record.
    /// </summary>
    Task<long> StartRunAsync(string triggerSource, string? correlationId, CancellationToken cancellationToken);

    /// <summary>
    /// Completes ETL run with final counters.
    /// </summary>
    Task CompleteRunAsync(
        long runId,
        string status,
        int totalRecords,
        int validRecords,
        int invalidRecords,
        int sentToApi,
        int apiFailures,
        int retryCount,
        long p95LatencyMs,
        long totalDurationMs,
        int slaCompliantCount,
        int failedBatches,
        decimal errorRate,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves last snapshot hash for supplier.
    /// </summary>
    Task<string?> GetLastSnapshotHashAsync(string supplierId, CancellationToken cancellationToken);

    /// <summary>
    /// Checks whether supplier id remains unique in persisted storage.
    /// </summary>
    /// <param name="supplierId">Supplier identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if zero or one records exist for supplier id; otherwise false.</returns>
    Task<bool> IsSupplierIdUniqueInDatabaseAsync(string supplierId, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves supplier change history snapshots.
    /// </summary>
    Task<IReadOnlyCollection<string>> GetSupplierHistoryAsync(string supplierId, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves pending retry suppliers eligible for current run.
    /// </summary>
    /// <param name="runId">Current run identifier.</param>
    /// <param name="maxRows">Maximum rows to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Retry dispatch items.</returns>
    Task<IReadOnlyCollection<SupplierDispatchItem>> GetPendingRetriesAsync(long runId, int maxRows, CancellationToken cancellationToken);

    /// <summary>
    /// Updates supplier retry state based on dispatch outcome.
    /// </summary>
    /// <param name="runId">Current run id.</param>
    /// <param name="supplierId">Supplier identifier.</param>
    /// <param name="isSuccess">Dispatch success indicator.</param>
    /// <param name="failureReason">Failure reason when unsuccessful.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetRetryStateAsync(long runId, string supplierId, bool isSuccess, string? failureReason, CancellationToken cancellationToken);
}
