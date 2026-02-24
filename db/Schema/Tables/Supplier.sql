CREATE TABLE [dbo].[Supplier]
(
    [Id]                  BIGINT         NOT NULL IDENTITY(1, 1),
    [SupplierId]          NVARCHAR(100)  NOT NULL,
    [SupplierName]        NVARCHAR(300)  NULL,
    [BankAccountName]     NVARCHAR(200)  NULL,
    [BankAccountNumber]   NVARCHAR(100)  NULL,
    [BankRoutingNumber]   NVARCHAR(100)  NULL,
    [BankCountryCode]     NVARCHAR(10)   NULL,
    [AddressLine1]        NVARCHAR(300)  NULL,
    [AddressLine2]        NVARCHAR(300)  NULL,
    [City]                NVARCHAR(100)  NULL,
    [StateProvince]       NVARCHAR(100)  NULL,
    [PostalCode]          NVARCHAR(30)   NULL,
    [CountryCode]         NVARCHAR(10)   NULL,
    [LastSnapshotHash]    NVARCHAR(128)  NULL,
    [DeliveryStatus]      NVARCHAR(50)   NOT NULL CONSTRAINT [DF_Supplier_DeliveryStatus] DEFAULT ('NOT_SENT'),
    [RetryAttemptCount]   INT            NOT NULL CONSTRAINT [DF_Supplier_RetryAttemptCount] DEFAULT (0),
    [LastRetryAt]         DATETIME2(7)   NULL,
    [NextRetryAt]         DATETIME2(7)   NULL,
    [LastDeliveredAt]     DATETIME2(7)   NULL,
    [LastSeenRunId]       BIGINT         NULL,
    [CreatedAt]           DATETIME2(7)   NOT NULL CONSTRAINT [DF_Supplier_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt]           DATETIME2(7)   NOT NULL CONSTRAINT [DF_Supplier_UpdatedAt] DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_Supplier] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Supplier_SupplierEtlRun] FOREIGN KEY ([LastSeenRunId]) REFERENCES [dbo].[SupplierEtlRun] ([Id])
);
GO

CREATE UNIQUE INDEX [UX_Supplier_SupplierId] ON [dbo].[Supplier] ([SupplierId]);
GO
CREATE INDEX [IX_Supplier_DeliveryStatus] ON [dbo].[Supplier] ([DeliveryStatus]);
GO
CREATE INDEX [IX_Supplier_LastSeenRunId] ON [dbo].[Supplier] ([LastSeenRunId]);
GO
CREATE INDEX [IX_Supplier_PendingRetry] ON [dbo].[Supplier] ([DeliveryStatus], [NextRetryAt], [RetryAttemptCount]);
GO
