using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "ETL API", Version = "v1" });
});

builder.Services.AddScoped(_ =>
    new SqlConnection(builder.Configuration.GetConnectionString("SqlConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// ── ETL Jobs ────────────────────────────────────────────────────────────────

app.MapGet("/api/etl-jobs", async (SqlConnection db) =>
{
    await db.OpenAsync();
    await using var cmd = new SqlCommand(
        "SELECT [Id],[Name],[Description],[CronSchedule],[IsEnabled],[CreatedAt] FROM [dbo].[EtlJob]", db);
    await using var reader = await cmd.ExecuteReaderAsync();
    var jobs = new List<object>();
    while (await reader.ReadAsync())
    {
        jobs.Add(new
        {
            id           = reader.GetInt32(0),
            name         = reader.GetString(1),
            description  = reader.IsDBNull(2) ? null : reader.GetString(2),
            cronSchedule = reader.GetString(3),
            isEnabled    = reader.GetBoolean(4),
            createdAt    = reader.GetDateTime(5)
        });
    }
    return Results.Ok(jobs);
})
.WithName("GetEtlJobs")
.WithOpenApi();

app.MapGet("/api/etl-jobs/{id}/logs", async (int id, SqlConnection db) =>
{
    await db.OpenAsync();
    await using var cmd = new SqlCommand(
        "SELECT [Id],[Status],[StartedAt],[FinishedAt],[RowsProcessed],[ErrorMessage] " +
        "FROM [dbo].[EtlJobLog] WHERE [EtlJobId] = @id ORDER BY [Id] DESC", db);
    cmd.Parameters.AddWithValue("@id", id);
    await using var reader = await cmd.ExecuteReaderAsync();
    var logs = new List<object>();
    while (await reader.ReadAsync())
    {
        logs.Add(new
        {
            id            = reader.GetInt64(0),
            status        = reader.GetString(1),
            startedAt     = reader.IsDBNull(2) ? (DateTime?)null : reader.GetDateTime(2),
            finishedAt    = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3),
            rowsProcessed = reader.IsDBNull(4) ? (int?)null : reader.GetInt32(4),
            errorMessage  = reader.IsDBNull(5) ? null : reader.GetString(5)
        });
    }
    return Results.Ok(logs);
})
.WithName("GetEtlJobLogs")
.WithOpenApi();

// ── Supplier ETL Dashboard ───────────────────────────────────────────────────

app.MapGet("/api/supplier-etl/runs", async (DateTime? fromUtc, DateTime? toUtc, SqlConnection db) =>
{
    await db.OpenAsync();
    await using var cmd = new SqlCommand(
        "SELECT [RunId],[TriggerSource],[CorrelationId],[Status],[StartedAt],[FinishedAt],[RecordsIn],[RecordsValidated],[RecordsSent],[RecordsFailed],[RecordsSkipped],[ValidationFailureCount],[ApiFailureCount],[RetryCount],[FailedBatchesCount],[P95LatencyMs],[SlaCompliancePct],[TotalProcessingMs],[ErrorRatePct],[DurationMs] " +
        "FROM [dbo].[vw_SupplierEtlRunStatistics] " +
        "WHERE (@fromUtc IS NULL OR [StartedAt] >= @fromUtc) AND (@toUtc IS NULL OR [StartedAt] <= @toUtc) " +
        "ORDER BY [StartedAt] DESC", db);
    cmd.Parameters.AddWithValue("@fromUtc", (object?)fromUtc ?? DBNull.Value);
    cmd.Parameters.AddWithValue("@toUtc", (object?)toUtc ?? DBNull.Value);

    await using var reader = await cmd.ExecuteReaderAsync();
    var runs = new List<object>();
    while (await reader.ReadAsync())
    {
        runs.Add(new
        {
            runId = reader.GetInt64(0),
            triggerSource = reader.GetString(1),
            correlationId = reader.IsDBNull(2) ? null : reader.GetString(2),
            status = reader.GetString(3),
            startedAt = reader.GetDateTime(4),
            finishedAt = reader.IsDBNull(5) ? (DateTime?)null : reader.GetDateTime(5),
            recordsIn = reader.GetInt32(6),
            recordsValidated = reader.GetInt32(7),
            recordsSent = reader.GetInt32(8),
            recordsFailed = reader.GetInt32(9),
            recordsSkipped = reader.GetInt32(10),
            validationFailureCount = reader.GetInt32(11),
            apiFailureCount = reader.GetInt32(12),
            retryCount = reader.GetInt32(13),
            failedBatchesCount = reader.GetInt32(14),
            p95LatencyMs = reader.IsDBNull(15) ? (int?)null : reader.GetInt32(15),
            slaCompliancePct = reader.IsDBNull(16) ? (decimal?)null : reader.GetDecimal(16),
            totalProcessingMs = reader.IsDBNull(17) ? (int?)null : reader.GetInt32(17),
            errorRatePct = reader.GetDecimal(18),
            durationMs = reader.IsDBNull(19) ? (int?)null : reader.GetInt32(19)
        });
    }

    return Results.Ok(runs);
})
.WithName("GetSupplierEtlRuns")
.WithOpenApi();

app.MapGet("/api/supplier-etl/retry-queue", async (SqlConnection db) =>
{
    await db.OpenAsync();
    await using var cmd = new SqlCommand(
        "SELECT [SupplierId],[SupplierName],[DeliveryStatus],[RetryAttemptCount],[LastRetryAt],[NextRetryAt],[LastSeenRunId],[UpdatedAt] FROM [dbo].[vw_SupplierRetryQueue] ORDER BY [UpdatedAt] DESC", db);

    await using var reader = await cmd.ExecuteReaderAsync();
    var retries = new List<object>();
    while (await reader.ReadAsync())
    {
        retries.Add(new
        {
            supplierId = reader.GetString(0),
            supplierName = reader.IsDBNull(1) ? null : reader.GetString(1),
            deliveryStatus = reader.GetString(2),
            retryAttemptCount = reader.GetInt32(3),
            lastRetryAt = reader.IsDBNull(4) ? (DateTime?)null : reader.GetDateTime(4),
            nextRetryAt = reader.IsDBNull(5) ? (DateTime?)null : reader.GetDateTime(5),
            lastSeenRunId = reader.IsDBNull(6) ? (long?)null : reader.GetInt64(6),
            updatedAt = reader.GetDateTime(7)
        });
    }

    return Results.Ok(retries);
})
.WithName("GetSupplierRetryQueue")
.WithOpenApi();

app.MapGet("/api/supplier-etl/suppliers/{supplierId}/history", async (string supplierId, SqlConnection db) =>
{
    await db.OpenAsync();
    await using var cmd = new SqlCommand(
        "SELECT [SupplierId],[SnapshotId],[EtlRunId],[ChangeType],[SnapshotHash],[SnapshotPayload],[ChangedAt] " +
        "FROM [dbo].[vw_SupplierChangeHistory] WHERE [SupplierId] = @supplierId ORDER BY [ChangedAt] ASC", db);
    cmd.Parameters.AddWithValue("@supplierId", supplierId);

    await using var reader = await cmd.ExecuteReaderAsync();
    var history = new List<object>();
    while (await reader.ReadAsync())
    {
        history.Add(new
        {
            supplierId = reader.GetString(0),
            snapshotId = reader.GetInt64(1),
            etlRunId = reader.GetInt64(2),
            changeType = reader.GetString(3),
            snapshotHash = reader.IsDBNull(4) ? null : reader.GetString(4),
            snapshotPayload = reader.GetString(5),
            changedAt = reader.GetDateTime(6)
        });
    }

    return Results.Ok(history);
})
.WithName("GetSupplierChangeHistory")
.WithOpenApi();

app.Run();
