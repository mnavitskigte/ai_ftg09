CREATE PROCEDURE [dbo].[usp_LogEtlJobResult]
    @LogId         BIGINT,
    @Status        NVARCHAR(50),
    @ErrorMessage  NVARCHAR(MAX) = NULL,
    @RowsProcessed INT           = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE [dbo].[EtlJobLog]
    SET    [Status]        = @Status,
           [FinishedAt]    = SYSUTCDATETIME(),
           [ErrorMessage]  = @ErrorMessage,
           [RowsProcessed] = @RowsProcessed
    WHERE  [Id] = @LogId;
END;
GO
