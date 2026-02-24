using EtlFunction.Models;

namespace EtlFunction.Contracts;

/// <summary>
/// Retry queue abstraction.
/// </summary>
public interface IRetryQueueService
{
    /// <summary>
    /// Gets pending retry records.
    /// </summary>
    Task<IReadOnlyCollection<SupplierDispatchItem>> GetPendingRetriesAsync(long runId, CancellationToken cancellationToken);

    /// <summary>
    /// Marks supplier dispatch as failed and queues retry.
    /// </summary>
    Task UpsertRetryAsync(long runId, DispatchResult dispatchResult, CancellationToken cancellationToken);

    /// <summary>
    /// Clears pending retry state after successful dispatch.
    /// </summary>
    Task ClearRetryAsync(string supplierId, CancellationToken cancellationToken);
}
