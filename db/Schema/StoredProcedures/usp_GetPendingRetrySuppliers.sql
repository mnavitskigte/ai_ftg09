CREATE PROCEDURE [dbo].[usp_GetPendingRetrySuppliers]
    @MaxRows INT = 100
AS
BEGIN
    SET NOCOUNT ON;

    IF (@MaxRows IS NULL OR @MaxRows < 1)
    BEGIN
        SET @MaxRows = 100;
    END;

    ;WITH [RetryQueue] AS
    (
        SELECT
            s.[SupplierId],
            s.[SupplierName],
            s.[RetryAttemptCount],
            s.[LastRetryAt],
            s.[NextRetryAt],
            s.[LastSeenRunId],
            s.[UpdatedAt],
            ROW_NUMBER() OVER
            (
                ORDER BY ISNULL(s.[NextRetryAt], CAST('1900-01-01T00:00:00' AS DATETIME2(7))) ASC,
                         s.[UpdatedAt] ASC
            ) AS [RowNum]
        FROM [dbo].[Supplier] AS s
        WHERE s.[DeliveryStatus] = 'PENDING_RETRY'
          AND (s.[NextRetryAt] IS NULL OR s.[NextRetryAt] <= SYSUTCDATETIME())
    )
    SELECT
        q.[SupplierId],
        q.[SupplierName],
        q.[RetryAttemptCount],
        q.[LastRetryAt],
        q.[NextRetryAt],
        q.[LastSeenRunId],
        q.[UpdatedAt]
    FROM [RetryQueue] AS q
    WHERE q.[RowNum] <= @MaxRows
    ORDER BY q.[RowNum] ASC;
END;
GO
