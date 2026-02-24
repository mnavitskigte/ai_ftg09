using EtlFunction.Models;

namespace EtlFunction.Contracts;

/// <summary>
/// Metrics service abstraction.
/// </summary>
public interface IEtlMetricsService
{
    /// <summary>
    /// Calculates ETL KPI values for a run.
    /// </summary>
    /// <param name="runId">Current run id.</param>
    /// <param name="totalRecords">Total records received.</param>
    /// <param name="validRecords">Valid records count.</param>
    /// <param name="invalidRecords">Invalid records count.</param>
    /// <param name="dispatchResults">Per-record dispatch outcomes.</param>
    /// <param name="startedAtUtc">Run start time.</param>
    /// <returns>Metrics result object.</returns>
    EtlRunMetrics Calculate(
        long runId,
        int totalRecords,
        int validRecords,
        int invalidRecords,
        IReadOnlyCollection<DispatchResult> dispatchResults,
        DateTime startedAtUtc);
}
