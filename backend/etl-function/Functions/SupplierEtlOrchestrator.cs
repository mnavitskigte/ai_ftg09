using EtlFunction.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;

namespace EtlFunction.Functions;

/// <summary>
/// Durable orchestrator for supplier ETL workflow.
/// </summary>
public static class SupplierEtlOrchestrator
{
    /// <summary>
    /// Coordinates ETL pipeline steps using durable fan-out/fan-in pattern.
    /// </summary>
    [Function(nameof(SupplierEtlOrchestrator))]
    public static async Task<EtlRunMetrics> RunAsync(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var runContext = context.GetInput<EtlRunContext>() ?? new EtlRunContext();

        var startedRun = await context.CallActivityAsync<EtlRunContext>(nameof(SupplierActivities.StartRunActivity), runContext);

        var suppliers = await context.CallActivityAsync<IReadOnlyCollection<SupplierRecord>>(nameof(SupplierActivities.FetchSuppliersActivity), startedRun);

        var validation = await context.CallActivityAsync<(IReadOnlyCollection<SupplierRecord> Valid, IReadOnlyCollection<ValidationFailureRecord> Invalid)>(
            nameof(SupplierActivities.ValidateSuppliersActivity),
            (startedRun.RunId, suppliers));

        var classified = await context.CallActivityAsync<IReadOnlyCollection<SupplierDispatchItem>>(
            nameof(SupplierActivities.ClassifyAndPersistActivity),
            (startedRun.RunId, validation.Valid));

        var pendingRetry = await context.CallActivityAsync<IReadOnlyCollection<SupplierDispatchItem>>(
            nameof(SupplierActivities.LoadPendingRetryActivity),
            startedRun.RunId);

        var dispatchQueue = BuildDispatchQueue(classified, pendingRetry);

        var dispatchTasks = new List<Task<DispatchResult>>();
        foreach (var item in dispatchQueue)
        {
            dispatchTasks.Add(context.CallActivityAsync<DispatchResult>(nameof(SupplierActivities.DispatchSupplierActivity), (startedRun.RunId, item)));
        }

        var results = await Task.WhenAll(dispatchTasks);

        var metrics = await context.CallActivityAsync<EtlRunMetrics>(
            nameof(SupplierActivities.CalculateAndPersistMetricsActivity),
            (startedRun, suppliers.Count, validation.Valid.Count, validation.Invalid.Count, (IReadOnlyCollection<DispatchResult>)results));

        await context.CallActivityAsync(nameof(SupplierActivities.CompleteRunActivity), (startedRun.RunId, metrics));

        return metrics;
    }

    private static IReadOnlyCollection<SupplierDispatchItem> BuildDispatchQueue(
        IReadOnlyCollection<SupplierDispatchItem> classified,
        IReadOnlyCollection<SupplierDispatchItem> pendingRetry)
    {
        // Build dispatch queue with supplier-level deduplication, prioritizing classified items over retry items.
        var dispatchQueue = new List<SupplierDispatchItem>();
        var seenSupplierIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in classified)
        {
            if (seenSupplierIds.Add(item.Supplier.SupplierId))
            {
                dispatchQueue.Add(item);
            }
        }

        foreach (var item in pendingRetry)
        {
            if (seenSupplierIds.Add(item.Supplier.SupplierId))
            {
                dispatchQueue.Add(item);
            }
        }

        return dispatchQueue;
    }
}
