CREATE PROCEDURE [dbo].[usp_StartEtlJob]
    @EtlJobId INT,
    @LogId    BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO [dbo].[EtlJobLog] ([EtlJobId], [Status], [StartedAt])
    VALUES (@EtlJobId, 'Running', SYSUTCDATETIME());

    SET @LogId = SCOPE_IDENTITY();
END;
GO
