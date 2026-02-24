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

app.Run();
