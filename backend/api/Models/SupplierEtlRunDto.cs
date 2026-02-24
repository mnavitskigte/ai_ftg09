namespace EtlApi.Models;

public sealed class SupplierEtlRunDto
{
    public long RunId { get; set; }

    public string TriggerSource { get; set; } = string.Empty;

    public string? CorrelationId { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime StartedAt { get; set; }

    public DateTime? FinishedAt { get; set; }

    public int RecordsIn { get; set; }

    public int RecordsValidated { get; set; }

    public int RecordsSent { get; set; }

    public int RecordsFailed { get; set; }

    public int RecordsSkipped { get; set; }

    public int ValidationFailureCount { get; set; }

    public int ApiFailureCount { get; set; }

    public int RetryCount { get; set; }

    public int FailedBatchesCount { get; set; }

    public int? P95LatencyMs { get; set; }

    public decimal? SlaCompliancePct { get; set; }

    public int? TotalProcessingMs { get; set; }

    public decimal ErrorRatePct { get; set; }

    public int? DurationMs { get; set; }
}
