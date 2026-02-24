using System.Data;
using System.Text.Json;
using Dapper;
using EtlFunction.Contracts;
using EtlFunction.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EtlFunction.Repositories;

/// <summary>
/// SQL repository implementation for supplier ETL persistence.
/// </summary>
public sealed class SupplierRepository : ISupplierRepository, IAuditRepository
{
    private readonly string _connectionString;
    private readonly ILogger<SupplierRepository> _logger;

    /// <summary>
    /// Creates a new <see cref="SupplierRepository"/> instance.
    /// </summary>
    public SupplierRepository(IConfiguration configuration, ILogger<SupplierRepository> logger)
    {
        _connectionString = configuration.GetConnectionString("SqlConnection")
            ?? configuration["SqlConnectionString"]
            ?? throw new InvalidOperationException("SQL connection string is not configured.");
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<long> StartRunAsync(string triggerSource, string? correlationId, CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var parameters = new DynamicParameters();
        parameters.Add("@TriggerSource", triggerSource, DbType.String, ParameterDirection.Input);
        parameters.Add("@CorrelationId", correlationId, DbType.String, ParameterDirection.Input);
        parameters.Add("@RunId", dbType: DbType.Int64, direction: ParameterDirection.Output);

        var command = new CommandDefinition(
            "dbo.usp_StartSupplierEtlRun",
            parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        await connection.ExecuteAsync(command);
        return parameters.Get<long>("@RunId");
    }

    /// <inheritdoc />
    public async Task CompleteRunAsync(
        long runId,
        string status,
        int totalRecords,
        int validRecords,
        int invalidRecords,
        int sentToApi,
        int apiFailures,
        int retryCount,
        long p95LatencyMs,
        long totalDurationMs,
        int slaCompliantCount,
        int failedBatches,
        decimal errorRate,
        CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var p95LatencyMsInt = p95LatencyMs > int.MaxValue ? int.MaxValue : (int)p95LatencyMs;
        var totalProcessingMs = totalDurationMs > int.MaxValue ? int.MaxValue : (int)totalDurationMs;

        decimal? slaCompliancePct = null;
        if (sentToApi > 0 && slaCompliantCount >= 0)
        {
            slaCompliancePct = Math.Round((decimal)slaCompliantCount * 100m / sentToApi, 2, MidpointRounding.AwayFromZero);
        }

        var parameters = new DynamicParameters();
        parameters.Add("@RunId", runId, DbType.Int64);
        parameters.Add("@Status", status, DbType.String);
        parameters.Add("@RecordsIn", totalRecords, DbType.Int32);
        parameters.Add("@RecordsValidated", validRecords, DbType.Int32);
        parameters.Add("@RecordsSent", sentToApi, DbType.Int32);
        parameters.Add("@RecordsFailed", apiFailures, DbType.Int32);
        parameters.Add("@RecordsSkipped", invalidRecords, DbType.Int32);
        parameters.Add("@ValidationFailureCount", invalidRecords, DbType.Int32);
        parameters.Add("@ApiFailureCount", apiFailures, DbType.Int32);
        parameters.Add("@P95LatencyMs", p95LatencyMsInt, DbType.Int32);
        parameters.Add("@SlaCompliancePct", slaCompliancePct, DbType.Decimal);
        parameters.Add("@FailedBatchesCount", failedBatches, DbType.Int32);
        parameters.Add("@TotalProcessingMs", totalProcessingMs, DbType.Int32);
        parameters.Add("@RetryCount", retryCount, DbType.Int32);
        parameters.Add("@ErrorMessage", dbType: DbType.String, value: null);

        var command = new CommandDefinition(
            "dbo.usp_CompleteSupplierEtlRun",
            parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        await connection.ExecuteAsync(command);
    }

    /// <inheritdoc />
    public async Task<string?> GetLastSnapshotHashAsync(string supplierId, CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        const string sql = "SELECT [LastSnapshotHash] FROM [dbo].[Supplier] WHERE [SupplierId] = @SupplierId";
        var command = new CommandDefinition(sql, new { SupplierId = supplierId }, cancellationToken: cancellationToken);
        return await connection.QueryFirstOrDefaultAsync<string?>(command);
    }

    /// <inheritdoc />
    public async Task<bool> IsSupplierIdUniqueInDatabaseAsync(string supplierId, CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        const string sql = "SELECT COUNT(1) FROM [dbo].[Supplier] WHERE [SupplierId] = @SupplierId";
        var command = new CommandDefinition(sql, new { SupplierId = supplierId }, cancellationToken: cancellationToken);
        var count = await connection.ExecuteScalarAsync<int>(command);
        return count <= 1;
    }

    /// <inheritdoc />
    public async Task UpsertSupplierAsync(
        long runId,
        SupplierRecord record,
        SupplierChangeClassification classification,
        string rowHash,
        CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var parameters = new DynamicParameters();
        parameters.Add("@EtlRunId", runId, DbType.Int64);
        parameters.Add("@SupplierId", record.SupplierId, DbType.String);
        parameters.Add("@SupplierName", record.Name, DbType.String);
        parameters.Add("@BankAccountName", record.BankAccountName, DbType.String);
        parameters.Add("@BankAccountNumber", record.BankAccountNumber, DbType.String);
        parameters.Add("@BankRoutingNumber", record.BankRoutingNumber, DbType.String);
        parameters.Add("@BankCountryCode", dbType: DbType.String, value: null);
        parameters.Add("@AddressLine1", record.AddressLine1, DbType.String);
        parameters.Add("@AddressLine2", dbType: DbType.String, value: null);
        parameters.Add("@City", record.City, DbType.String);
        parameters.Add("@StateProvince", dbType: DbType.String, value: null);
        parameters.Add("@PostalCode", dbType: DbType.String, value: null);
        parameters.Add("@CountryCode", record.CountryCode, DbType.String);
        parameters.Add("@SnapshotHash", rowHash, DbType.String);
        parameters.Add("@SnapshotPayload", record.RawPayload ?? JsonSerializer.Serialize(record), DbType.String);
        parameters.Add("@ChangeType", dbType: DbType.String, direction: ParameterDirection.Output, size: 50);

        var command = new CommandDefinition(
            "dbo.usp_UpsertSupplierWithSnapshot",
            parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        await connection.ExecuteAsync(command);

        var dbChangeType = parameters.Get<string>("@ChangeType");
        if (!string.Equals(dbChangeType, classification.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Supplier change classification mismatch for SupplierId {SupplierId}. Calculated={Calculated}, Database={Database}",
                record.SupplierId,
                classification,
                dbChangeType);
        }
    }

    /// <inheritdoc />
    public async Task LogValidationErrorAsync(long runId, ValidationFailureRecord failure, CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var parameters = new DynamicParameters();
        parameters.Add("@EtlRunId", runId, DbType.Int64);
        parameters.Add("@SupplierId", failure.SupplierId, DbType.String);
        parameters.Add("@FailureCode", "VALIDATION_ERROR", DbType.String);
        parameters.Add("@FailureMessage", failure.ErrorReason, DbType.String);
        parameters.Add("@Payload", failure.RawPayload, DbType.String);

        var command = new CommandDefinition(
            "dbo.usp_LogSupplierValidationFailure",
            parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        await connection.ExecuteAsync(command);
    }

    /// <inheritdoc />
    public async Task LogApiCallAsync(
        long runId,
        string supplierId,
        string requestPayload,
        string responsePayload,
        int statusCode,
        bool isSuccess,
        long durationMs,
        string? failureReason,
        CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var latencyMs = durationMs > int.MaxValue ? int.MaxValue : (int)durationMs;

        var parameters = new DynamicParameters();
        parameters.Add("@SupplierId", supplierId, DbType.String);
        parameters.Add("@EtlRunId", runId, DbType.Int64);
        parameters.Add("@RequestPayload", requestPayload, DbType.String);
        parameters.Add("@ResponsePayload", responsePayload, DbType.String);
        parameters.Add("@HttpStatusCode", statusCode, DbType.Int32);
        parameters.Add("@LatencyMs", latencyMs, DbType.Int32);
        parameters.Add("@IsSuccess", isSuccess, DbType.Boolean);
        parameters.Add("@FailureReason", failureReason, DbType.String);

        var command = new CommandDefinition(
            "dbo.usp_LogSupplierApiDispatch",
            parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        await connection.ExecuteAsync(command);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<string>> GetSupplierHistoryAsync(string supplierId, CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
SELECT [SnapshotPayload]
FROM [dbo].[vw_SupplierChangeHistory]
WHERE [SupplierId] = @SupplierId
ORDER BY [ChangedAt] ASC";

        var command = new CommandDefinition(sql, new { SupplierId = supplierId }, cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<string>(command);
        return rows.ToArray();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<SupplierDispatchItem>> GetPendingRetriesAsync(long runId, int maxRows, CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var parameters = new DynamicParameters();
        parameters.Add("@MaxRows", maxRows, DbType.Int32);

        var command = new CommandDefinition(
            "dbo.usp_GetPendingRetrySuppliers",
            parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        var rows = await connection.QueryAsync<PendingRetryRow>(command);

        return rows.Select(row => new SupplierDispatchItem
        {
            RunId = runId,
            IsRetry = true,
            Classification = SupplierChangeClassification.Updated,
            Supplier = new SupplierRecord
            {
                SupplierId = row.SupplierId,
                Name = row.SupplierName
            }
        }).ToArray();
    }

    /// <inheritdoc />
    public async Task SetRetryStateAsync(long runId, string supplierId, bool isSuccess, string? failureReason, CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
UPDATE [dbo].[Supplier]
SET [DeliveryStatus] = CASE WHEN @IsSuccess = 1 THEN 'DELIVERED' ELSE 'PENDING_RETRY' END,
    [RetryAttemptCount] = CASE WHEN @IsSuccess = 1 THEN 0 ELSE [RetryAttemptCount] + 1 END,
    [LastRetryAt] = CASE WHEN @IsSuccess = 1 THEN [LastRetryAt] ELSE SYSUTCDATETIME() END,
    [NextRetryAt] = CASE WHEN @IsSuccess = 1 THEN NULL ELSE DATEADD(MINUTE, 15, SYSUTCDATETIME()) END,
    [LastDeliveredAt] = CASE WHEN @IsSuccess = 1 THEN SYSUTCDATETIME() ELSE [LastDeliveredAt] END,
    [UpdatedAt] = SYSUTCDATETIME(),
            [LastSeenRunId] = CASE WHEN @RunId > 0 THEN @RunId ELSE [LastSeenRunId] END
WHERE [SupplierId] = @SupplierId;";

        var command = new CommandDefinition(
            sql,
            new
            {
                RunId = runId,
                SupplierId = supplierId,
                IsSuccess = isSuccess
            },
            cancellationToken: cancellationToken);

        await connection.ExecuteAsync(command);
    }

    private SqlConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }

    private sealed class PendingRetryRow
    {
        public string SupplierId { get; set; } = string.Empty;

        public string? SupplierName { get; set; }
    }
}
