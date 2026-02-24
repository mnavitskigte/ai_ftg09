using EtlFunction.Contracts;
using EtlFunction.Models;
using Microsoft.Extensions.Configuration;

namespace EtlFunction.Services;

/// <summary>
/// ETL metrics calculation service.
/// </summary>
public sealed class EtlMetricsService : IEtlMetricsService
{
    private readonly int _slaWindowSeconds;

    /// <summary>
    /// Creates a new <see cref="EtlMetricsService"/> instance.
    /// </summary>
    public EtlMetricsService(IConfiguration configuration)
    {
        _slaWindowSeconds = configuration.GetValue<int?>("Sla:WindowSeconds") ?? 300;
    }

    /// <inheritdoc />
    public EtlRunMetrics Calculate(
        long runId,
        int totalRecords,
        int validRecords,
        int invalidRecords,
        IReadOnlyCollection<DispatchResult> dispatchResults,
        DateTime startedAtUtc)
    {
        var totalDispatch = dispatchResults.Count;
        var apiFailures = dispatchResults.Count(result => !result.IsSuccess);
        var retryCount = apiFailures;

        var errorRate = totalRecords > 0
            ? Math.Round((invalidRecords + apiFailures) / (decimal)totalRecords, 4, MidpointRounding.AwayFromZero)
            : 0m;

        var durations = dispatchResults
            .Select(result => result.DurationMs)
            .Where(duration => duration >= 0)
            .OrderBy(duration => duration)
            .ToArray();

        long p95LatencyMs = 0;
        if (durations.Length > 0)
        {
            var p95Index = (int)Math.Ceiling(durations.Length * 0.95d) - 1;
            p95Index = Math.Clamp(p95Index, 0, durations.Length - 1);
            p95LatencyMs = durations[p95Index];
        }

        var slaThresholdMs = _slaWindowSeconds * 1000L;
        var slaCompliantCount = dispatchResults.Count(result => result.IsSuccess && result.DurationMs <= slaThresholdMs);

        var failedBatches = apiFailures > 0 ? 1 : 0;
        var totalDurationMs = Math.Max(0L, (long)(DateTime.UtcNow - startedAtUtc).TotalMilliseconds);

        return new EtlRunMetrics
        {
            TotalRecords = totalRecords,
            ValidRecords = validRecords,
            InvalidRecords = invalidRecords,
            SentToApi = totalDispatch,
            ApiFailures = apiFailures,
            RetryCount = retryCount,
            ErrorRate = errorRate,
            P95LatencyMs = p95LatencyMs,
            SlaCompliantCount = slaCompliantCount,
            FailedBatches = failedBatches,
            TotalDurationMs = totalDurationMs
        };
    }
}
