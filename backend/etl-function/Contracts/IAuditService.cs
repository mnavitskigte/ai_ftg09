using EtlFunction.Models;

namespace EtlFunction.Contracts;

/// <summary>
/// Audit service abstraction for shadow/audit logs.
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Writes validation failure audit row.
    /// </summary>
    Task WriteValidationAuditAsync(long runId, ValidationFailureRecord failure, CancellationToken cancellationToken);

    /// <summary>
    /// Writes supplier snapshot audit row.
    /// </summary>
    Task WriteSupplierAuditAsync(
        long runId,
        SupplierRecord record,
        SupplierChangeClassification classification,
        CancellationToken cancellationToken);

    /// <summary>
    /// Writes destination API call audit row.
    /// </summary>
    Task WriteApiAuditAsync(long runId, DispatchResult dispatchResult, CancellationToken cancellationToken);
}
