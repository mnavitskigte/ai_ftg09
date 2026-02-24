namespace EtlApi.Models;

public sealed class SupplierChangeHistoryItemDto
{
    public string SupplierId { get; set; } = string.Empty;

    public long SnapshotId { get; set; }

    public long EtlRunId { get; set; }

    public string ChangeType { get; set; } = string.Empty;

    public string? SnapshotHash { get; set; }

    public string SnapshotPayload { get; set; } = string.Empty;

    public DateTime ChangedAt { get; set; }
}
