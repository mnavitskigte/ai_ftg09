using EtlFunction.Models;

namespace EtlFunction.Contracts;

/// <summary>
/// Supplier validation abstraction.
/// </summary>
public interface ISupplierValidator
{
    /// <summary>
    /// Validates supplier records and returns valid and invalid sets.
    /// </summary>
    /// <param name="runId">Current run id.</param>
    /// <param name="records">Input supplier records.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation output.</returns>
    Task<(IReadOnlyCollection<SupplierRecord> Valid, IReadOnlyCollection<ValidationFailureRecord> Invalid)> ValidateAsync(
        long runId,
        IReadOnlyCollection<SupplierRecord> records,
        CancellationToken cancellationToken);
}
