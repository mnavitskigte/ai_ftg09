CREATE TABLE [dbo].[SupplierValidationLog]
(
    [Id]               BIGINT         NOT NULL IDENTITY(1, 1),
    [EtlRunId]         BIGINT         NOT NULL,
    [SupplierId]       NVARCHAR(100)  NULL,
    [FailureCode]      NVARCHAR(100)  NOT NULL,
    [FailureMessage]   NVARCHAR(1000) NOT NULL,
    [Payload]          NVARCHAR(MAX)  NULL,
    [CreatedAt]        DATETIME2(7)   NOT NULL CONSTRAINT [DF_SupplierValidationLog_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_SupplierValidationLog] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_SupplierValidationLog_SupplierEtlRun] FOREIGN KEY ([EtlRunId]) REFERENCES [dbo].[SupplierEtlRun] ([Id])
);
GO

CREATE INDEX [IX_SupplierValidationLog_EtlRunId] ON [dbo].[SupplierValidationLog] ([EtlRunId]);
GO
CREATE INDEX [IX_SupplierValidationLog_SupplierId] ON [dbo].[SupplierValidationLog] ([SupplierId]);
GO
