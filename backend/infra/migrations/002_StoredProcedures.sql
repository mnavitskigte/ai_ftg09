-- 002_StoredProcedures.sql
-- TODO: inject schema

CREATE PROCEDURE [dbo].[usp_StartEtlRun]
    @TriggerSource NVARCHAR(100),
    @RunId BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO [dbo].[EtlRuns] ([TriggerSource], [Status])
    VALUES (@TriggerSource, 'Running');

    SET @RunId = SCOPE_IDENTITY();
END;
GO

CREATE PROCEDURE [dbo].[usp_CompleteEtlRun]
    @RunId BIGINT,
    @Status NVARCHAR(30),
    @TotalRecords INT,
    @ValidRecords INT,
    @InvalidRecords INT,
    @NewRecords INT,
    @UpdatedRecords INT,
    @UnchangedRecords INT,
    @SentToApi INT,
    @ApiFailures INT,
    @RetryCount INT,
    @ErrorRate DECIMAL(5,4),
    @P95LatencyMs BIGINT,
    @TotalDurationMs BIGINT,
    @SlaCompliantCount INT,
    @FailedBatches INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE [dbo].[EtlRuns]
    SET [CompletedAt] = SYSUTCDATETIME(),
        [Status] = @Status,
        [TotalRecords] = @TotalRecords,
        [ValidRecords] = @ValidRecords,
        [InvalidRecords] = @InvalidRecords,
        [NewRecords] = @NewRecords,
        [UpdatedRecords] = @UpdatedRecords,
        [UnchangedRecords] = @UnchangedRecords,
        [SentToApi] = @SentToApi,
        [ApiFailures] = @ApiFailures,
        [RetryCount] = @RetryCount,
        [ErrorRate] = @ErrorRate,
        [P95LatencyMs] = @P95LatencyMs,
        [TotalDurationMs] = @TotalDurationMs,
        [SlaCompliantCount] = @SlaCompliantCount,
        [FailedBatches] = @FailedBatches
    WHERE [RunId] = @RunId;
END;
GO

CREATE PROCEDURE [dbo].[usp_UpsertSupplierPendingRetry]
    @SupplierId NVARCHAR(100),
    @OriginalRunId BIGINT,
    @LastErrorMessage NVARCHAR(1000) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM [dbo].[SupplierPendingRetry] WHERE [SupplierId] = @SupplierId)
    BEGIN
        UPDATE [dbo].[SupplierPendingRetry]
        SET [RetryCount] = [RetryCount] + 1,
            [FailedAt] = SYSUTCDATETIME(),
            [LastErrorMessage] = @LastErrorMessage
        WHERE [SupplierId] = @SupplierId;
    END
    ELSE
    BEGIN
        INSERT INTO [dbo].[SupplierPendingRetry] ([SupplierId], [OriginalRunId], [LastErrorMessage])
        VALUES (@SupplierId, @OriginalRunId, @LastErrorMessage);
    END;
END;
GO

CREATE PROCEDURE [dbo].[usp_DeleteSupplierPendingRetry]
    @SupplierId NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM [dbo].[SupplierPendingRetry]
    WHERE [SupplierId] = @SupplierId;
END;
GO
