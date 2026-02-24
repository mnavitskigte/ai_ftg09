using EtlFunction.Models;

namespace EtlFunction.Contracts;

/// <summary>
/// Destination REST API client abstraction.
/// </summary>
public interface IDestinationApiClient
{
    /// <summary>
    /// Sends supplier record to destination API.
    /// </summary>
    /// <param name="item">Dispatch item.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dispatch result.</returns>
    Task<DispatchResult> SendSupplierAsync(SupplierDispatchItem item, CancellationToken cancellationToken);
}
