CREATE PROCEDURE [dbo].[usp_LogSupplierValidationFailure]
    @EtlRunId        BIGINT,
    @SupplierId      NVARCHAR(100)  = NULL,
    @FailureCode     NVARCHAR(100),
    @FailureMessage  NVARCHAR(1000),
    @Payload         NVARCHAR(MAX)  = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO [dbo].[SupplierValidationLog] ([EtlRunId], [SupplierId], [FailureCode], [FailureMessage], [Payload])
    VALUES (@EtlRunId, @SupplierId, @FailureCode, @FailureMessage, @Payload);
END;
GO
