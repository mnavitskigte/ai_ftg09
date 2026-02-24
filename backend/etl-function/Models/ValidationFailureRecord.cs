namespace EtlFunction.Models;

/// <summary>
/// Validation failure payload for persistence.
/// </summary>
public sealed class ValidationFailureRecord
{
    /// <summary>
    /// Supplier identifier from payload.
    /// </summary>
    public string? SupplierId { get; set; }

    /// <summary>
    /// Validation failure reason.
    /// </summary>
    public string ErrorReason { get; set; } = string.Empty;

    /// <summary>
    /// Raw payload for diagnostics.
    /// </summary>
    public string? RawPayload { get; set; }
}
