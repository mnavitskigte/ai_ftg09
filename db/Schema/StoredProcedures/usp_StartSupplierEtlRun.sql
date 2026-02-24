CREATE PROCEDURE [dbo].[usp_StartSupplierEtlRun]
    @TriggerSource NVARCHAR(100),
    @CorrelationId NVARCHAR(100) = NULL,
    @RunId         BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO [dbo].[SupplierEtlRun] ([TriggerSource], [CorrelationId], [Status], [StartedAt])
    VALUES (@TriggerSource, @CorrelationId, 'Running', SYSUTCDATETIME());

    SET @RunId = SCOPE_IDENTITY();
END;
GO
