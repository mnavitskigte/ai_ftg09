using EtlApi.Data;
using EtlApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EtlApi.Controllers;

[ApiController]
[Authorize]
[Route("api/etl-jobs")]
public sealed class EtlJobsController : ControllerBase
{
    private readonly ISupplierEtlReadRepository _repository;

    public EtlJobsController(ISupplierEtlReadRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<IActionResult> GetJobs([FromQuery] bool isMock = false, CancellationToken cancellationToken = default)
    {
        if (isMock)
        {
            return Ok(BuildMockJobs());
        }

        var jobs = await _repository.GetEtlJobsAsync(cancellationToken);
        return Ok(jobs);
    }

    [HttpGet("{id:int}/logs")]
    public async Task<IActionResult> GetJobLogs(int id, [FromQuery] bool isMock = false, CancellationToken cancellationToken = default)
    {
        if (isMock)
        {
            return Ok(BuildMockJobLogs(id));
        }

        var logs = await _repository.GetEtlJobLogsAsync(id, cancellationToken);
        return Ok(logs);
    }

    private static IReadOnlyCollection<EtlJobDto> BuildMockJobs()
    {
        var random = Random.Shared;
        var count = random.Next(5, 10);
        var jobs = new List<EtlJobDto>(count);

        for (var i = 1; i <= count; i++)
        {
            jobs.Add(new EtlJobDto
            {
                Id = i,
                Name = $"Mock Job {i}",
                Description = "Generated mock ETL job",
                CronSchedule = "0 */30 * * * *",
                IsEnabled = random.Next(0, 2) == 1,
                CreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 90))
            });
        }

        return jobs;
    }

    private static IReadOnlyCollection<EtlJobLogDto> BuildMockJobLogs(int jobId)
    {
        var random = Random.Shared;
        var count = random.Next(8, 18);
        var logs = new List<EtlJobLogDto>(count);
        var statuses = new[] { "Completed", "Failed", "Running", "PartialFailure" };

        for (var i = 0; i < count; i++)
        {
            var startedAt = DateTime.UtcNow.AddHours(-(i + 1) * 3);
            var status = statuses[random.Next(statuses.Length)];
            var finishedAt = status == "Running" ? (DateTime?)null : startedAt.AddMinutes(random.Next(3, 45));

            logs.Add(new EtlJobLogDto
            {
                Id = (jobId * 10_000L) + i + 1,
                Status = status,
                StartedAt = startedAt,
                FinishedAt = finishedAt,
                RowsProcessed = status == "Running" ? null : random.Next(120, 5_000),
                ErrorMessage = status == "Failed" ? "Mock downstream timeout." : null
            });
        }

        return logs.OrderByDescending(x => x.Id).ToList();
    }
}
