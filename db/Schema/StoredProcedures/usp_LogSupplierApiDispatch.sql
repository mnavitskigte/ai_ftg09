CREATE PROCEDURE [dbo].[usp_LogSupplierApiDispatch]
    @SupplierId       NVARCHAR(100),
    @EtlRunId         BIGINT,
    @RequestPayload   NVARCHAR(MAX) = NULL,
    @ResponsePayload  NVARCHAR(MAX) = NULL,
    @HttpStatusCode   INT           = NULL,
    @LatencyMs        INT           = NULL,
    @IsSuccess        BIT,
    @FailureReason    NVARCHAR(1000) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @SupplierPk BIGINT;

    SELECT @SupplierPk = s.[Id]
    FROM [dbo].[Supplier] AS s
    WHERE s.[SupplierId] = @SupplierId;

    IF @SupplierPk IS NULL
    BEGIN
        THROW 50001, 'Supplier not found for API dispatch log.', 1;
    END;

    INSERT INTO [dbo].[SupplierApiDispatchLog]
    (
        [SupplierId],
        [EtlRunId],
        [RequestPayload],
        [ResponsePayload],
        [HttpStatusCode],
        [LatencyMs],
        [IsSuccess],
        [FailureReason]
    )
    VALUES
    (
        @SupplierPk,
        @EtlRunId,
        @RequestPayload,
        @ResponsePayload,
        @HttpStatusCode,
        @LatencyMs,
        @IsSuccess,
        @FailureReason
    );

    UPDATE [dbo].[Supplier]
    SET    [DeliveryStatus] = CASE WHEN @IsSuccess = 1 THEN 'DELIVERED' ELSE 'PENDING_RETRY' END,
            [RetryAttemptCount] = CASE WHEN @IsSuccess = 1 THEN 0 ELSE [RetryAttemptCount] + 1 END,
            [LastRetryAt] = CASE WHEN @IsSuccess = 1 THEN [LastRetryAt] ELSE SYSUTCDATETIME() END,
            [NextRetryAt] = CASE 
                       WHEN @IsSuccess = 1 THEN NULL
                       ELSE DATEADD(MINUTE, 15, SYSUTCDATETIME())
                      END,
           [LastDeliveredAt] = CASE WHEN @IsSuccess = 1 THEN SYSUTCDATETIME() ELSE [LastDeliveredAt] END,
           [UpdatedAt] = SYSUTCDATETIME(),
           [LastSeenRunId] = @EtlRunId
    WHERE  [Id] = @SupplierPk;
END;
GO
