CREATE PROCEDURE [dbo].[usp_CompleteSupplierEtlRun]
    @RunId                   BIGINT,
    @Status                  NVARCHAR(50),
    @RecordsIn               INT,
    @RecordsValidated        INT,
    @RecordsSent             INT,
    @RecordsFailed           INT,
    @RecordsSkipped          INT,
    @ValidationFailureCount  INT,
    @ApiFailureCount         INT,
    @P95LatencyMs            INT           = NULL,
    @SlaCompliancePct        DECIMAL(5, 2) = NULL,
    @FailedBatchesCount      INT           = 0,
    @TotalProcessingMs       INT           = NULL,
    @RetryCount              INT           = 0,
    @ErrorMessage            NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE [dbo].[SupplierEtlRun]
    SET    [Status]                 = @Status,
           [FinishedAt]             = SYSUTCDATETIME(),
           [RecordsIn]              = @RecordsIn,
           [RecordsValidated]       = @RecordsValidated,
           [RecordsSent]            = @RecordsSent,
           [RecordsFailed]          = @RecordsFailed,
           [RecordsSkipped]         = @RecordsSkipped,
           [ValidationFailureCount] = @ValidationFailureCount,
           [ApiFailureCount]        = @ApiFailureCount,
           [P95LatencyMs]           = @P95LatencyMs,
           [SlaCompliancePct]       = @SlaCompliancePct,
           [FailedBatchesCount]     = @FailedBatchesCount,
           [TotalProcessingMs]      = @TotalProcessingMs,
           [RetryCount]             = @RetryCount,
           [ErrorMessage]           = @ErrorMessage
    WHERE  [Id] = @RunId;
END;
GO
