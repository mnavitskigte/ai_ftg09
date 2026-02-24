CREATE VIEW [dbo].[vw_SupplierEtlRunStatistics]
AS
SELECT
    r.[Id]                 AS [RunId],
    r.[TriggerSource],
    r.[CorrelationId],
    r.[Status],
    r.[StartedAt],
    r.[FinishedAt],
    r.[RecordsIn],
    r.[RecordsValidated],
    r.[RecordsSent],
    r.[RecordsFailed],
    r.[RecordsSkipped],
    r.[ValidationFailureCount],
    r.[ApiFailureCount],
    r.[RetryCount],
    r.[FailedBatchesCount],
    r.[P95LatencyMs],
    r.[SlaCompliancePct],
    r.[TotalProcessingMs],
    CASE
        WHEN r.[RecordsIn] = 0 THEN CAST(0.00 AS DECIMAL(10, 2))
        ELSE CAST((100.0 * (r.[ValidationFailureCount] + r.[ApiFailureCount])) / NULLIF(r.[RecordsIn], 0) AS DECIMAL(10, 2))
    END                    AS [ErrorRatePct],
    DATEDIFF(MILLISECOND, r.[StartedAt], r.[FinishedAt]) AS [DurationMs]
FROM [dbo].[SupplierEtlRun] AS r;
GO
