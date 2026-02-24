namespace EtlFunction.Models;

/// <summary>
/// ETL run metadata and counters.
/// </summary>
public sealed class EtlRunContext
{
    /// <summary>
    /// Database run identifier.
    /// </summary>
    public long RunId { get; set; }

    /// <summary>
    /// Trigger source for this run.
    /// </summary>
    public string TriggerSource { get; set; } = string.Empty;

    /// <summary>
    /// Optional correlation identifier.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Current run status.
    /// </summary>
    public string Status { get; set; } = "Running";

    /// <summary>
    /// Run start time (UTC).
    /// </summary>
    public DateTime StartedAtUtc { get; set; }
}
