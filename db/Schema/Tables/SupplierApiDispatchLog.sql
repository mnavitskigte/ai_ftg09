CREATE TABLE [dbo].[SupplierApiDispatchLog]
(
    [Id]               BIGINT         NOT NULL IDENTITY(1, 1),
    [SupplierId]       BIGINT         NOT NULL,
    [EtlRunId]         BIGINT         NOT NULL,
    [RequestPayload]   NVARCHAR(MAX)  NULL,
    [ResponsePayload]  NVARCHAR(MAX)  NULL,
    [HttpStatusCode]   INT            NULL,
    [LatencyMs]        INT            NULL,
    [IsSuccess]        BIT            NOT NULL,
    [FailureReason]    NVARCHAR(1000) NULL,
    [AttemptedAt]      DATETIME2(7)   NOT NULL CONSTRAINT [DF_SupplierApiDispatchLog_AttemptedAt] DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_SupplierApiDispatchLog] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_SupplierApiDispatchLog_Supplier] FOREIGN KEY ([SupplierId]) REFERENCES [dbo].[Supplier] ([Id]),
    CONSTRAINT [FK_SupplierApiDispatchLog_SupplierEtlRun] FOREIGN KEY ([EtlRunId]) REFERENCES [dbo].[SupplierEtlRun] ([Id])
);
GO

CREATE INDEX [IX_SupplierApiDispatchLog_SupplierId_AttemptedAt] ON [dbo].[SupplierApiDispatchLog] ([SupplierId], [AttemptedAt] DESC);
GO
CREATE INDEX [IX_SupplierApiDispatchLog_EtlRunId] ON [dbo].[SupplierApiDispatchLog] ([EtlRunId]);
GO
CREATE INDEX [IX_SupplierApiDispatchLog_IsSuccess] ON [dbo].[SupplierApiDispatchLog] ([IsSuccess]);
GO
