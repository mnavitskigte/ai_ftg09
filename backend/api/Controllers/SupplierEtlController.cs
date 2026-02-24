using EtlApi.Data;
using EtlApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EtlApi.Controllers;

[ApiController]
[Authorize]
[Route("api/supplier-etl")]
public sealed class SupplierEtlController : ControllerBase
{
    private readonly ISupplierEtlReadRepository _repository;

    public SupplierEtlController(ISupplierEtlReadRepository repository)
    {
        _repository = repository;
    }

    [HttpGet("runs")]
    public async Task<IActionResult> GetRuns([FromQuery] DateTime? fromUtc, [FromQuery] DateTime? toUtc, [FromQuery] bool isMock = false, CancellationToken cancellationToken = default)
    {
        if (isMock)
        {
            return Ok(BuildMockRuns(fromUtc, toUtc));
        }

        var runs = await _repository.GetSupplierRunsAsync(fromUtc, toUtc, cancellationToken);
        return Ok(runs);
    }

    [HttpGet("retry-queue")]
    public async Task<IActionResult> GetRetryQueue([FromQuery] bool isMock = false, CancellationToken cancellationToken = default)
    {
        if (isMock)
        {
            return Ok(BuildMockRetryQueue());
        }

        var retryQueue = await _repository.GetRetryQueueAsync(cancellationToken);
        return Ok(retryQueue);
    }

    [HttpGet("suppliers/{supplierId}/history")]
    public async Task<IActionResult> GetSupplierHistory(string supplierId, [FromQuery] bool isMock = false, CancellationToken cancellationToken = default)
    {
        if (isMock)
        {
            return Ok(BuildMockSupplierHistory(supplierId));
        }

        var history = await _repository.GetSupplierHistoryAsync(supplierId, cancellationToken);
        return Ok(history);
    }

    private static IReadOnlyCollection<SupplierEtlRunDto> BuildMockRuns(DateTime? fromUtc, DateTime? toUtc)
    {
        var random = Random.Shared;
        var statuses = new[] { "Completed", "Failed", "PartialFailure", "Running" };
        var from = fromUtc ?? DateTime.UtcNow.AddDays(-15);
        var to = toUtc ?? DateTime.UtcNow;

        if (to < from)
        {
            (from, to) = (to, from);
        }

        var spanHours = Math.Max(1, (int)(to - from).TotalHours);
        var count = random.Next(12, 26);
        var runs = new List<SupplierEtlRunDto>(count);

        for (var i = 0; i < count; i++)
        {
            var startedAt = from.AddHours(random.Next(0, spanHours));
            var status = statuses[random.Next(statuses.Length)];
            var recordsIn = random.Next(200, 4000);
            var recordsSent = random.Next((int)(recordsIn * 0.5), recordsIn + 1);
            var recordsFailed = Math.Max(0, recordsIn - recordsSent);

            runs.Add(new SupplierEtlRunDto
            {
                RunId = 80_000 + i,
                TriggerSource = random.Next(0, 2) == 0 ? "Scheduler" : "Webhook",
                CorrelationId = $"mock-corr-{Guid.NewGuid():N}"[..16],
                Status = status,
                StartedAt = startedAt,
                FinishedAt = status == "Running" ? null : startedAt.AddMinutes(random.Next(5, 45)),
                RecordsIn = recordsIn,
                RecordsValidated = recordsIn - random.Next(0, 50),
                RecordsSent = recordsSent,
                RecordsFailed = recordsFailed,
                RecordsSkipped = random.Next(0, 30),
                ValidationFailureCount = random.Next(0, 40),
                ApiFailureCount = status == "Failed" ? random.Next(5, 30) : random.Next(0, 8),
                RetryCount = random.Next(0, 12),
                FailedBatchesCount = status == "Failed" ? random.Next(1, 5) : random.Next(0, 2),
                P95LatencyMs = random.Next(50, 900),
                SlaCompliancePct = Math.Round((decimal)random.NextDouble() * 15 + 85, 2),
                TotalProcessingMs = random.Next(500, 120000),
                ErrorRatePct = Math.Round((decimal)recordsFailed / Math.Max(1, recordsIn) * 100m, 2),
                DurationMs = random.Next(1000, 180000)
            });
        }

        return runs.OrderByDescending(x => x.StartedAt).ToList();
    }

    private static IReadOnlyCollection<SupplierRetryQueueItemDto> BuildMockRetryQueue()
    {
        var random = Random.Shared;
        var count = random.Next(6, 15);
        var list = new List<SupplierRetryQueueItemDto>(count);
        var statuses = new[] { "Pending", "Scheduled", "Retrying" };

        for (var i = 0; i < count; i++)
        {
            list.Add(new SupplierRetryQueueItemDto
            {
                SupplierId = $"SUP-{1000 + i}",
                SupplierName = $"Mock Supplier {i + 1}",
                DeliveryStatus = statuses[random.Next(statuses.Length)],
                RetryAttemptCount = random.Next(1, 8),
                LastRetryAt = DateTime.UtcNow.AddMinutes(-random.Next(5, 1800)),
                NextRetryAt = DateTime.UtcNow.AddMinutes(random.Next(5, 180)),
                LastSeenRunId = 90_000 + random.Next(1, 120),
                UpdatedAt = DateTime.UtcNow.AddMinutes(-random.Next(1, 300))
            });
        }

        return list.OrderByDescending(x => x.UpdatedAt).ToList();
    }

    private static IReadOnlyCollection<SupplierChangeHistoryItemDto> BuildMockSupplierHistory(string supplierId)
    {
        var random = Random.Shared;
        var count = random.Next(8, 16);
        var history = new List<SupplierChangeHistoryItemDto>(count);
        var changeTypes = new[] { "NEW", "UPDATED", "RETRY" };

        for (var i = 0; i < count; i++)
        {
            history.Add(new SupplierChangeHistoryItemDto
            {
                SupplierId = supplierId,
                SnapshotId = 500_000 + i,
                EtlRunId = 90_000 + i,
                ChangeType = changeTypes[random.Next(changeTypes.Length)],
                SnapshotHash = Guid.NewGuid().ToString("N")[..16],
                SnapshotPayload = $"{{\"supplierId\":\"{supplierId}\",\"revision\":{i + 1}}}",
                ChangedAt = DateTime.UtcNow.AddHours(-(count - i) * 8)
            });
        }

        return history.OrderByDescending(x => x.ChangedAt).ToList();
    }
}
