using EtlFunction.Contracts;
using EtlFunction.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace EtlFunction.Functions;

/// <summary>
/// Durable activity functions for supplier ETL pipeline.
/// </summary>
public sealed class SupplierActivities
{
    private readonly ISupplierRepository _supplierRepository;
    private readonly ISoapSourceClient _soapSourceClient;
    private readonly ISupplierValidator _supplierValidator;
    private readonly IChangeDetectionService _changeDetectionService;
    private readonly IDestinationApiClient _destinationApiClient;
    private readonly IEtlMetricsService _etlMetricsService;
    private readonly IAuditService _auditService;
    private readonly IRetryQueueService _retryQueueService;
    private readonly ILogger<SupplierActivities> _logger;

    /// <summary>
    /// Creates a new <see cref="SupplierActivities"/> instance.
    /// </summary>
    public SupplierActivities(
        ISupplierRepository supplierRepository,
        ISoapSourceClient soapSourceClient,
        ISupplierValidator supplierValidator,
        IChangeDetectionService changeDetectionService,
        IDestinationApiClient destinationApiClient,
        IEtlMetricsService etlMetricsService,
        IAuditService auditService,
        IRetryQueueService retryQueueService,
        ILogger<SupplierActivities> logger)
    {
        _supplierRepository = supplierRepository;
        _soapSourceClient = soapSourceClient;
        _supplierValidator = supplierValidator;
        _changeDetectionService = changeDetectionService;
        _destinationApiClient = destinationApiClient;
        _etlMetricsService = etlMetricsService;
        _auditService = auditService;
        _retryQueueService = retryQueueService;
        _logger = logger;
    }

    /// <summary>
    /// Starts ETL run audit record.
    /// </summary>
    [Function(nameof(StartRunActivity))]
    public async Task<EtlRunContext> StartRunActivity([ActivityTrigger] EtlRunContext input, CancellationToken cancellationToken)
    {
        var runId = await _supplierRepository.StartRunAsync(input.TriggerSource, input.CorrelationId, cancellationToken);
        input.RunId = runId;
        return input;
    }

    /// <summary>
    /// Fetches full supplier list from source.
    /// </summary>
    [Function(nameof(FetchSuppliersActivity))]
    public async Task<IReadOnlyCollection<SupplierRecord>> FetchSuppliersActivity([ActivityTrigger] EtlRunContext runContext, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Run {RunId}: starting SOAP extraction", runContext.RunId);

        var suppliers = await _soapSourceClient.GetSuppliersAsync(cancellationToken);

        _logger.LogInformation("Run {RunId}: extracted {SupplierCount} suppliers from SOAP source", runContext.RunId, suppliers.Count);
        return suppliers;
    }

    /// <summary>
    /// Validates suppliers and logs invalid records.
    /// </summary>
    [Function(nameof(ValidateSuppliersActivity))]
    public Task<(IReadOnlyCollection<SupplierRecord> Valid, IReadOnlyCollection<ValidationFailureRecord> Invalid)> ValidateSuppliersActivity(
        [ActivityTrigger] (long RunId, IReadOnlyCollection<SupplierRecord> Suppliers) input,
        CancellationToken cancellationToken)
    {
        return _supplierValidator.ValidateAsync(input.RunId, input.Suppliers, cancellationToken);
    }

    /// <summary>
    /// Classifies supplier changes and persists supplier + audit snapshot.
    /// </summary>
    [Function(nameof(ClassifyAndPersistActivity))]
    public async Task<IReadOnlyCollection<SupplierDispatchItem>> ClassifyAndPersistActivity(
        [ActivityTrigger] (long RunId, IReadOnlyCollection<SupplierRecord> Suppliers) input,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Classifying and persisting suppliers for run {RunId}", input.RunId);

        var dispatchItems = new List<SupplierDispatchItem>();

        foreach (var supplier in input.Suppliers)
        {
            try
            {
                var previousHash = await _supplierRepository.GetLastSnapshotHashAsync(supplier.SupplierId, cancellationToken);
                var classification = _changeDetectionService.Classify(supplier, previousHash);

                await _auditService.WriteSupplierAuditAsync(input.RunId, supplier, classification, cancellationToken);

                if (classification is SupplierChangeClassification.New or SupplierChangeClassification.Updated)
                {
                    dispatchItems.Add(new SupplierDispatchItem
                    {
                        RunId = input.RunId,
                        Supplier = supplier,
                        Classification = classification,
                        IsRetry = false
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to classify/persist supplier {SupplierId} in run {RunId}. Skipping supplier.",
                    supplier.SupplierId,
                    input.RunId);
            }
        }

        return dispatchItems;
    }

    /// <summary>
    /// Loads pending retries for run merge.
    /// </summary>
    [Function(nameof(LoadPendingRetryActivity))]
    public Task<IReadOnlyCollection<SupplierDispatchItem>> LoadPendingRetryActivity([ActivityTrigger] long runId, CancellationToken cancellationToken)
    {
        return _retryQueueService.GetPendingRetriesAsync(runId, cancellationToken);
    }

    /// <summary>
    /// Dispatches one supplier to destination API.
    /// </summary>
    [Function(nameof(DispatchSupplierActivity))]
    public async Task<DispatchResult> DispatchSupplierActivity([ActivityTrigger] (long RunId, SupplierDispatchItem Item) input, CancellationToken cancellationToken)
    {
        var dispatchResult = await _destinationApiClient.SendSupplierAsync(input.Item, cancellationToken);

        await _auditService.WriteApiAuditAsync(input.RunId, dispatchResult, cancellationToken);

        if (dispatchResult.IsSuccess)
        {
            await _retryQueueService.ClearRetryAsync(dispatchResult.SupplierId, cancellationToken);
        }
        else
        {
            await _retryQueueService.UpsertRetryAsync(input.RunId, dispatchResult, cancellationToken);
        }

        return dispatchResult;
    }

    /// <summary>
    /// Calculates and persists metrics.
    /// </summary>
    [Function(nameof(CalculateAndPersistMetricsActivity))]
    public Task<EtlRunMetrics> CalculateAndPersistMetricsActivity(
        [ActivityTrigger] (EtlRunContext RunContext, int TotalRecords, int ValidRecords, int InvalidRecords, IReadOnlyCollection<DispatchResult> DispatchResults) input,
        CancellationToken cancellationToken)
    {
        var metrics = _etlMetricsService.Calculate(
            input.RunContext.RunId,
            input.TotalRecords,
            input.ValidRecords,
            input.InvalidRecords,
            input.DispatchResults,
            input.RunContext.StartedAtUtc);

        return Task.FromResult(metrics);
    }

    /// <summary>
    /// Completes run with final status.
    /// </summary>
    [Function(nameof(CompleteRunActivity))]
    public Task CompleteRunActivity([ActivityTrigger] (long RunId, EtlRunMetrics Metrics) input, CancellationToken cancellationToken)
    {
        var status = input.Metrics.ApiFailures > 0 ? "PartialFailure" : "Completed";
        return _supplierRepository.CompleteRunAsync(
            input.RunId,
            status,
            input.Metrics.TotalRecords,
            input.Metrics.ValidRecords,
            input.Metrics.InvalidRecords,
            input.Metrics.SentToApi,
            input.Metrics.ApiFailures,
            input.Metrics.RetryCount,
            input.Metrics.P95LatencyMs,
            input.Metrics.TotalDurationMs,
            input.Metrics.SlaCompliantCount,
            input.Metrics.FailedBatches,
            input.Metrics.ErrorRate,
            cancellationToken);
    }
}
