CREATE VIEW [dbo].[vw_SupplierRetryQueue]
AS
SELECT
    s.[SupplierId],
    s.[SupplierName],
    s.[DeliveryStatus],
    s.[RetryAttemptCount],
    s.[LastRetryAt],
    s.[NextRetryAt],
    s.[LastSeenRunId],
    s.[UpdatedAt]
FROM [dbo].[Supplier] AS s
WHERE s.[DeliveryStatus] = 'PENDING_RETRY';
GO
