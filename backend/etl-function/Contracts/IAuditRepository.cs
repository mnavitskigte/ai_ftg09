using EtlFunction.Models;

namespace EtlFunction.Contracts;

/// <summary>
/// Persistence abstraction for audit writes.
/// </summary>
public interface IAuditRepository
{
    /// <summary>
    /// Upserts supplier and writes snapshot where required.
    /// </summary>
    Task UpsertSupplierAsync(
        long runId,
        SupplierRecord record,
        SupplierChangeClassification classification,
        string rowHash,
        CancellationToken cancellationToken);

    /// <summary>
    /// Persists validation audit record.
    /// </summary>
    Task LogValidationErrorAsync(long runId, ValidationFailureRecord failure, CancellationToken cancellationToken);

    /// <summary>
    /// Persists outbound API audit record.
    /// </summary>
    Task LogApiCallAsync(
        long runId,
        string supplierId,
        string requestPayload,
        string responsePayload,
        int statusCode,
        bool isSuccess,
        long durationMs,
        string? failureReason,
        CancellationToken cancellationToken);
}
