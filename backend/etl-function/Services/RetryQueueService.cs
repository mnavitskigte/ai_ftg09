using EtlFunction.Contracts;
using EtlFunction.Models;
using Microsoft.Extensions.Logging;

namespace EtlFunction.Services;

/// <summary>
/// Retry queue service.
/// </summary>
public sealed class RetryQueueService : IRetryQueueService
{
    private readonly ISupplierRepository _supplierRepository;
    private readonly ILogger<RetryQueueService> _logger;

    /// <summary>
    /// Creates a new <see cref="RetryQueueService"/> instance.
    /// </summary>
    public RetryQueueService(ISupplierRepository supplierRepository, ILogger<RetryQueueService> logger)
    {
        _supplierRepository = supplierRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<SupplierDispatchItem>> GetPendingRetriesAsync(long runId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Loading pending retry suppliers for run {RunId}", runId);
        return await _supplierRepository.GetPendingRetriesAsync(runId, 100, cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpsertRetryAsync(long runId, DispatchResult dispatchResult, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Upserting retry entry for supplier {SupplierId}, run {RunId}", dispatchResult.SupplierId, runId);
        await _supplierRepository.SetRetryStateAsync(runId, dispatchResult.SupplierId, false, dispatchResult.FailureMessage, cancellationToken);
    }

    /// <inheritdoc />
    public async Task ClearRetryAsync(string supplierId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Clearing retry entry for supplier {SupplierId}", supplierId);
        await _supplierRepository.SetRetryStateAsync(0, supplierId, true, null, cancellationToken);
    }
}
