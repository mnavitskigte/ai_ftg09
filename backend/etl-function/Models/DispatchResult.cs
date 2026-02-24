namespace EtlFunction.Models;

/// <summary>
/// Dispatch outcome for one supplier.
/// </summary>
public sealed class DispatchResult
{
    /// <summary>
    /// Supplier identifier.
    /// </summary>
    public string SupplierId { get; set; } = string.Empty;

    /// <summary>
    /// Indicates outbound API success.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// HTTP status code when available.
    /// </summary>
    public int? HttpStatusCode { get; set; }

    /// <summary>
    /// Failure message when unsuccessful.
    /// </summary>
    public string? FailureMessage { get; set; }

    /// <summary>
    /// End-to-end processing latency in milliseconds.
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// Outbound request payload.
    /// </summary>
    public string? RequestPayload { get; set; }

    /// <summary>
    /// Destination response payload.
    /// </summary>
    public string? ResponsePayload { get; set; }
}
