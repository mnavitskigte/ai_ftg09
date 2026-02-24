CREATE TABLE [dbo].[SupplierSnapshot]
(
    [Id]               BIGINT         NOT NULL IDENTITY(1, 1),
    [SupplierId]       BIGINT         NOT NULL,
    [EtlRunId]         BIGINT         NOT NULL,
    [ChangeType]       NVARCHAR(50)   NOT NULL,  -- NEW, UPDATED
    [SnapshotHash]     NVARCHAR(128)  NULL,
    [SnapshotPayload]  NVARCHAR(MAX)  NOT NULL,
    [CreatedAt]        DATETIME2(7)   NOT NULL CONSTRAINT [DF_SupplierSnapshot_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_SupplierSnapshot] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_SupplierSnapshot_Supplier] FOREIGN KEY ([SupplierId]) REFERENCES [dbo].[Supplier] ([Id]),
    CONSTRAINT [FK_SupplierSnapshot_SupplierEtlRun] FOREIGN KEY ([EtlRunId]) REFERENCES [dbo].[SupplierEtlRun] ([Id])
);
GO

CREATE INDEX [IX_SupplierSnapshot_SupplierId_CreatedAt] ON [dbo].[SupplierSnapshot] ([SupplierId], [CreatedAt] DESC);
GO
CREATE INDEX [IX_SupplierSnapshot_EtlRunId] ON [dbo].[SupplierSnapshot] ([EtlRunId]);
GO
