CREATE VIEW [dbo].[vw_EtlJobStatus]
AS
SELECT
    j.[Id]           AS [JobId],
    j.[Name]         AS [JobName],
    j.[CronSchedule],
    j.[IsEnabled],
    l.[Id]           AS [LogId],
    l.[Status],
    l.[StartedAt],
    l.[FinishedAt],
    l.[RowsProcessed],
    l.[ErrorMessage],
    DATEDIFF(SECOND, l.[StartedAt], l.[FinishedAt]) AS [DurationSeconds]
FROM [dbo].[EtlJob]    AS j
LEFT JOIN [dbo].[EtlJobLog] AS l ON l.[EtlJobId] = j.[Id];
GO
