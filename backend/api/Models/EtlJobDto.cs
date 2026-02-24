namespace EtlApi.Models;

public sealed class EtlJobDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string CronSchedule { get; set; } = string.Empty;

    public bool IsEnabled { get; set; }

    public DateTime CreatedAt { get; set; }
}
