namespace EtlApi.Models;

public sealed class SupplierRetryQueueItemDto
{
    public string SupplierId { get; set; } = string.Empty;

    public string? SupplierName { get; set; }

    public string DeliveryStatus { get; set; } = string.Empty;

    public int RetryAttemptCount { get; set; }

    public DateTime? LastRetryAt { get; set; }

    public DateTime? NextRetryAt { get; set; }

    public long? LastSeenRunId { get; set; }

    public DateTime UpdatedAt { get; set; }
}
