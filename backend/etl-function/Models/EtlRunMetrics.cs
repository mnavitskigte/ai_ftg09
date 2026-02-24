namespace EtlFunction.Models;

/// <summary>
/// Calculated KPI values for ETL run.
/// </summary>
public sealed class EtlRunMetrics
{
    /// <summary>
    /// Total records from source.
    /// </summary>
    public int TotalRecords { get; set; }

    /// <summary>
    /// Valid records count.
    /// </summary>
    public int ValidRecords { get; set; }

    /// <summary>
    /// Invalid records count.
    /// </summary>
    public int InvalidRecords { get; set; }

    /// <summary>
    /// Sent to destination count.
    /// </summary>
    public int SentToApi { get; set; }

    /// <summary>
    /// API failures count.
    /// </summary>
    public int ApiFailures { get; set; }

    /// <summary>
    /// Pending retry count.
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Error rate value.
    /// </summary>
    public decimal ErrorRate { get; set; }

    /// <summary>
    /// P95 latency in milliseconds.
    /// </summary>
    public long P95LatencyMs { get; set; }

    /// <summary>
    /// SLA-compliant record count.
    /// </summary>
    public int SlaCompliantCount { get; set; }

    /// <summary>
    /// Failed batch count.
    /// </summary>
    public int FailedBatches { get; set; }

    /// <summary>
    /// Total run duration in milliseconds.
    /// </summary>
    public long TotalDurationMs { get; set; }
}
