using Dapper;
using EtlApi.Models;
using Microsoft.Data.SqlClient;

namespace EtlApi.Data;

public sealed class SupplierEtlReadRepository : ISupplierEtlReadRepository
{
    private readonly string _connectionString;

    public SupplierEtlReadRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("SqlConnection")
            ?? throw new InvalidOperationException("Connection string 'SqlConnection' is not configured.");
    }

    public async Task<bool> IsDatabaseReachableAsync(CancellationToken cancellationToken)
    {
        const string sql = "SELECT 1";

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            var result = await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, cancellationToken: cancellationToken));
            return result == 1;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IReadOnlyCollection<EtlJobDto>> GetEtlJobsAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT [Id],[Name],[Description],[CronSchedule],[IsEnabled],[CreatedAt]
            FROM [dbo].[EtlJob]
            ORDER BY [Id]
            """;

        await using var connection = new SqlConnection(_connectionString);
        var rows = await connection.QueryAsync<EtlJobDto>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return rows.ToList();
    }

    public async Task<IReadOnlyCollection<EtlJobLogDto>> GetEtlJobLogsAsync(int jobId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT [Id],[Status],[StartedAt],[FinishedAt],[RowsProcessed],[ErrorMessage]
            FROM [dbo].[EtlJobLog]
            WHERE [EtlJobId] = @jobId
            ORDER BY [Id] DESC
            """;

        await using var connection = new SqlConnection(_connectionString);
        var rows = await connection.QueryAsync<EtlJobLogDto>(new CommandDefinition(
            sql,
            parameters: new { jobId },
            cancellationToken: cancellationToken));
        return rows.ToList();
    }

    public async Task<IReadOnlyCollection<SupplierEtlRunDto>> GetSupplierRunsAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT [RunId],[TriggerSource],[CorrelationId],[Status],[StartedAt],[FinishedAt],[RecordsIn],[RecordsValidated],[RecordsSent],[RecordsFailed],[RecordsSkipped],[ValidationFailureCount],[ApiFailureCount],[RetryCount],[FailedBatchesCount],[P95LatencyMs],[SlaCompliancePct],[TotalProcessingMs],[ErrorRatePct],[DurationMs]
            FROM [dbo].[vw_SupplierEtlRunStatistics]
            WHERE (@fromUtc IS NULL OR [StartedAt] >= @fromUtc)
              AND (@toUtc IS NULL OR [StartedAt] <= @toUtc)
            ORDER BY [StartedAt] DESC
            """;

        await using var connection = new SqlConnection(_connectionString);
        var rows = await connection.QueryAsync<SupplierEtlRunDto>(new CommandDefinition(
            sql,
            parameters: new { fromUtc, toUtc },
            cancellationToken: cancellationToken));
        return rows.ToList();
    }

    public async Task<IReadOnlyCollection<SupplierRetryQueueItemDto>> GetRetryQueueAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT [SupplierId],[SupplierName],[DeliveryStatus],[RetryAttemptCount],[LastRetryAt],[NextRetryAt],[LastSeenRunId],[UpdatedAt]
            FROM [dbo].[vw_SupplierRetryQueue]
            ORDER BY [UpdatedAt] DESC
            """;

        await using var connection = new SqlConnection(_connectionString);
        var rows = await connection.QueryAsync<SupplierRetryQueueItemDto>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return rows.ToList();
    }

    public async Task<IReadOnlyCollection<SupplierChangeHistoryItemDto>> GetSupplierHistoryAsync(string supplierId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT [SupplierId],[SnapshotId],[EtlRunId],[ChangeType],[SnapshotHash],[SnapshotPayload],[ChangedAt]
            FROM [dbo].[vw_SupplierChangeHistory]
            WHERE [SupplierId] = @supplierId
            ORDER BY [ChangedAt] ASC
            """;

        await using var connection = new SqlConnection(_connectionString);
        var rows = await connection.QueryAsync<SupplierChangeHistoryItemDto>(new CommandDefinition(
            sql,
            parameters: new { supplierId },
            cancellationToken: cancellationToken));
        return rows.ToList();
    }
}
