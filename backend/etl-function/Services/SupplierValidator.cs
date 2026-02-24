using EtlFunction.Contracts;
using EtlFunction.Models;
using FluentValidation;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EtlFunction.Services;

/// <summary>
/// Supplier validation service.
/// </summary>
public sealed class SupplierValidator : ISupplierValidator
{
    private readonly IValidator<SupplierRecord> _validator;
    private readonly IAuditService _auditService;
    private readonly ISupplierRepository _supplierRepository;
    private readonly ILogger<SupplierValidator> _logger;

    /// <summary>
    /// Creates a new <see cref="SupplierValidator"/> instance.
    /// </summary>
    public SupplierValidator(
        IValidator<SupplierRecord> validator,
        IAuditService auditService,
        ISupplierRepository supplierRepository,
        ILogger<SupplierValidator> logger)
    {
        _validator = validator;
        _auditService = auditService;
        _supplierRepository = supplierRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyCollection<SupplierRecord> Valid, IReadOnlyCollection<ValidationFailureRecord> Invalid)> ValidateAsync(
        long runId,
        IReadOnlyCollection<SupplierRecord> records,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Validating {RecordCount} supplier records for run {RunId}", records.Count, runId);

        var validRecords = new List<SupplierRecord>();
        var invalidRecords = new List<ValidationFailureRecord>();
        var seenSupplierIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var dbUniquenessCache = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        foreach (var record in records)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var supplierId = (record.SupplierId ?? string.Empty).Trim();
            var rawPayload = record.RawPayload ?? JsonSerializer.Serialize(record);

            if (!seenSupplierIds.Add(supplierId))
            {
                var duplicateFailure = new ValidationFailureRecord
                {
                    SupplierId = supplierId,
                    ErrorReason = "SupplierId is duplicated within the current batch.",
                    RawPayload = rawPayload
                };

                invalidRecords.Add(duplicateFailure);
                await _auditService.WriteValidationAuditAsync(runId, duplicateFailure, cancellationToken);
                continue;
            }

            var validationResult = await _validator.ValidateAsync(record, cancellationToken);
            if (!validationResult.IsValid)
            {
                var reason = string.Join("; ", validationResult.Errors.Select(error => error.ErrorMessage).Distinct());
                var validationFailure = new ValidationFailureRecord
                {
                    SupplierId = supplierId,
                    ErrorReason = reason,
                    RawPayload = rawPayload
                };

                invalidRecords.Add(validationFailure);
                await _auditService.WriteValidationAuditAsync(runId, validationFailure, cancellationToken);
                continue;
            }

            if (!dbUniquenessCache.TryGetValue(supplierId, out var isUniqueInDatabase))
            {
                isUniqueInDatabase = await _supplierRepository.IsSupplierIdUniqueInDatabaseAsync(supplierId, cancellationToken);
                dbUniquenessCache[supplierId] = isUniqueInDatabase;
            }

            if (!isUniqueInDatabase)
            {
                var databaseFailure = new ValidationFailureRecord
                {
                    SupplierId = supplierId,
                    ErrorReason = "SupplierId is not unique in persisted database records.",
                    RawPayload = rawPayload
                };

                invalidRecords.Add(databaseFailure);
                await _auditService.WriteValidationAuditAsync(runId, databaseFailure, cancellationToken);
                continue;
            }

            validRecords.Add(record);
        }

        return (validRecords, invalidRecords);
    }
}
