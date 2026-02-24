using EtlFunction.Models;

namespace EtlFunction.Contracts;

/// <summary>
/// SOAP source client abstraction.
/// </summary>
public interface ISoapSourceClient
{
    /// <summary>
    /// Fetches full supplier list from SOAP source.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Supplier records from source.</returns>
    Task<IReadOnlyCollection<SupplierRecord>> GetSuppliersAsync(CancellationToken cancellationToken);
}
