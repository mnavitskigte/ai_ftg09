namespace EtlApi.Models;

public sealed class EtlJobLogDto
{
    public long Id { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime? StartedAt { get; set; }

    public DateTime? FinishedAt { get; set; }

    public int? RowsProcessed { get; set; }

    public string? ErrorMessage { get; set; }
}
