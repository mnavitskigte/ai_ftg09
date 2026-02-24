using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;

namespace EtlFunction.Functions;

public class RunEtlFunction
{
    private readonly ILogger<RunEtlFunction> _logger;
    private readonly string _connectionString;

    public RunEtlFunction(ILogger<RunEtlFunction> logger, IConfiguration configuration)
    {
        _logger = logger;
        _connectionString = configuration["SqlConnectionString"]
            ?? throw new InvalidOperationException("SqlConnectionString is not configured.");
    }

    // Runs every day at 02:00 UTC – adjust the CRON expression as needed.
    // Format: {second} {minute} {hour} {day} {month} {day-of-week}
    [Function(nameof(RunEtlFunction))]
    public async Task Run(
        [TimerTrigger("0 0 2 * * *")] TimerInfo timerInfo,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("RunEtlFunction started at {UtcNow}", DateTime.UtcNow);

        if (timerInfo.IsPastDue)
        {
            _logger.LogWarning("Timer is running late – the previous execution was missed.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // Fetch all enabled ETL jobs
        var jobs = await GetEnabledJobsAsync(connection, cancellationToken);

        foreach (var job in jobs)
        {
            long logId = 0;
            try
            {
                logId = await StartJobLogAsync(connection, job.Id, cancellationToken);
                int rowsProcessed = await ProcessJobAsync(job, cancellationToken);
                await CompleteJobLogAsync(connection, logId, "Completed", null, rowsProcessed, cancellationToken);
                _logger.LogInformation("Job '{JobName}' completed. Rows processed: {Rows}", job.Name, rowsProcessed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Job '{JobName}' failed.", job.Name);
                if (logId > 0)
                {
                    await CompleteJobLogAsync(connection, logId, "Failed", ex.Message, null, cancellationToken);
                }
            }
        }

        _logger.LogInformation("RunEtlFunction finished at {UtcNow}", DateTime.UtcNow);
    }

    // ---------------------------------------------------------------------------
    // Private helpers
    // ---------------------------------------------------------------------------

    private static async Task<List<EtlJobRecord>> GetEnabledJobsAsync(
        SqlConnection connection, CancellationToken ct)
    {
        const string sql = "SELECT [Id], [Name] FROM [dbo].[EtlJob] WHERE [IsEnabled] = 1";
        var result = new List<EtlJobRecord>();

        await using var cmd = new SqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            result.Add(new EtlJobRecord(reader.GetInt32(0), reader.GetString(1)));
        }

        return result;
    }

    private static async Task<long> StartJobLogAsync(
        SqlConnection connection, int jobId, CancellationToken ct)
    {
        await using var cmd = new SqlCommand("dbo.usp_StartEtlJob", connection)
        {
            CommandType = System.Data.CommandType.StoredProcedure
        };
        cmd.Parameters.AddWithValue("@EtlJobId", jobId);
        var outParam = cmd.Parameters.Add("@LogId", System.Data.SqlDbType.BigInt);
        outParam.Direction = System.Data.ParameterDirection.Output;

        await cmd.ExecuteNonQueryAsync(ct);
        return (long)outParam.Value;
    }

    private static async Task CompleteJobLogAsync(
        SqlConnection connection, long logId, string status,
        string? errorMessage, int? rowsProcessed, CancellationToken ct)
    {
        await using var cmd = new SqlCommand("dbo.usp_LogEtlJobResult", connection)
        {
            CommandType = System.Data.CommandType.StoredProcedure
        };
        cmd.Parameters.AddWithValue("@LogId", logId);
        cmd.Parameters.AddWithValue("@Status", status);
        cmd.Parameters.AddWithValue("@ErrorMessage", (object?)errorMessage ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@RowsProcessed", (object?)rowsProcessed ?? DBNull.Value);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    /// <summary>
    /// Replace this method with your actual ETL logic per job.
    /// </summary>
    private Task<int> ProcessJobAsync(EtlJobRecord job, CancellationToken ct)
    {
        _logger.LogInformation("Processing ETL job '{JobName}' (Id={JobId})", job.Name, job.Id);
        // TODO: implement ETL pipeline for each job
        return Task.FromResult(0);
    }

    private record EtlJobRecord(int Id, string Name);
}
