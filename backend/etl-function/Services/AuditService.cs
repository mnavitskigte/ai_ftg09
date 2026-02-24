using EtlFunction.Contracts;
using EtlFunction.Models;
using Microsoft.Extensions.Logging;

namespace EtlFunction.Services;

/// <summary>
/// Audit writer service.
/// </summary>
public sealed class AuditService : IAuditService
{
    private readonly IAuditRepository _auditRepository;
    private readonly IChangeDetectionService _changeDetectionService;
    private readonly ILogger<AuditService> _logger;

    /// <summary>
    /// Creates a new <see cref="AuditService"/> instance.
    /// </summary>
    public AuditService(
        IAuditRepository auditRepository,
        IChangeDetectionService changeDetectionService,
        ILogger<AuditService> logger)
    {
        _auditRepository = auditRepository;
        _changeDetectionService = changeDetectionService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task WriteValidationAuditAsync(long runId, ValidationFailureRecord failure, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Writing validation audit for supplier {SupplierId}, run {RunId}", failure.SupplierId, runId);
        await _auditRepository.LogValidationErrorAsync(runId, failure, cancellationToken);
    }

    /// <inheritdoc />
    public async Task WriteSupplierAuditAsync(
        long runId,
        SupplierRecord record,
        SupplierChangeClassification classification,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Writing supplier audit for supplier {SupplierId}, run {RunId}", record.SupplierId, runId);

        var rowHash = _changeDetectionService.ComputeRowHash(record);
        await _auditRepository.UpsertSupplierAsync(runId, record, classification, rowHash, cancellationToken);
    }

    /// <inheritdoc />
    public async Task WriteApiAuditAsync(long runId, DispatchResult dispatchResult, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Writing API audit for supplier {SupplierId}, run {RunId}", dispatchResult.SupplierId, runId);

        await _auditRepository.LogApiCallAsync(
            runId,
            dispatchResult.SupplierId,
            dispatchResult.RequestPayload ?? string.Empty,
            dispatchResult.ResponsePayload ?? string.Empty,
            dispatchResult.HttpStatusCode ?? 0,
            dispatchResult.IsSuccess,
            dispatchResult.DurationMs,
            dispatchResult.FailureMessage,
            cancellationToken);
    }
}
