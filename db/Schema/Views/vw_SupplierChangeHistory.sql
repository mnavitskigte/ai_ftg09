CREATE VIEW [dbo].[vw_SupplierChangeHistory]
AS
SELECT
    s.[SupplierId],
    ss.[Id]             AS [SnapshotId],
    ss.[EtlRunId],
    ss.[ChangeType],
    ss.[SnapshotHash],
    ss.[SnapshotPayload],
    ss.[CreatedAt]      AS [ChangedAt]
FROM [dbo].[SupplierSnapshot] AS ss
INNER JOIN [dbo].[Supplier] AS s ON s.[Id] = ss.[SupplierId];
GO
