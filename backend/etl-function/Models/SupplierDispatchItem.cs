namespace EtlFunction.Models;

/// <summary>
/// Dispatch queue item for destination API.
/// </summary>
public sealed class SupplierDispatchItem
{
    /// <summary>
    /// Run identifier.
    /// </summary>
    public long RunId { get; set; }

    /// <summary>
    /// Supplier payload.
    /// </summary>
    public SupplierRecord Supplier { get; set; } = new();

    /// <summary>
    /// Change classification.
    /// </summary>
    public SupplierChangeClassification Classification { get; set; }

    /// <summary>
    /// Indicates item originated from retry queue.
    /// </summary>
    public bool IsRetry { get; set; }
}
