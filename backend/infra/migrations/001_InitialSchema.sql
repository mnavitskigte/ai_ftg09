-- 001_InitialSchema.sql
-- TODO: inject schema

CREATE TABLE [dbo].[Suppliers]
(
    [SupplierId]           NVARCHAR(100)  NOT NULL,
    [Name]                 NVARCHAR(300)  NULL,
    [RowHash]              NVARCHAR(64)   NULL,
    [CreatedAt]            DATETIME2(7)   NOT NULL CONSTRAINT [DF_Suppliers_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt]            DATETIME2(7)   NOT NULL CONSTRAINT [DF_Suppliers_UpdatedAt] DEFAULT (SYSUTCDATETIME()),
    [LastRunId]            BIGINT         NULL,
    -- TODO: inject schema - add all supplier fields from source contract
    CONSTRAINT [PK_Suppliers] PRIMARY KEY CLUSTERED ([SupplierId] ASC)
);
GO

CREATE TABLE [dbo].[SuppliersAudit]
(
    [AuditId]              BIGINT         NOT NULL IDENTITY(1, 1),
    [SupplierId]           NVARCHAR(100)  NOT NULL,
    [RunId]                BIGINT         NOT NULL,
    [ChangeType]           NVARCHAR(20)   NOT NULL, -- INSERT | UPDATE
    [ChangedAt]            DATETIME2(7)   NOT NULL CONSTRAINT [DF_SuppliersAudit_ChangedAt] DEFAULT (SYSUTCDATETIME()),
    [SnapshotPayload]      NVARCHAR(MAX)  NOT NULL,
    -- TODO: inject schema - full supplier snapshot columns if required by data governance
    CONSTRAINT [PK_SuppliersAudit] PRIMARY KEY CLUSTERED ([AuditId] ASC)
);
GO

CREATE INDEX [IX_SuppliersAudit_SupplierId_ChangedAt]
    ON [dbo].[SuppliersAudit] ([SupplierId], [ChangedAt] DESC);
GO

CREATE TABLE [dbo].[SupplierApiCallLog]
(
    [Id]                   BIGINT         NOT NULL IDENTITY(1, 1),
    [SupplierId]           NVARCHAR(100)  NOT NULL,
    [RunId]                BIGINT         NOT NULL,
    [RequestPayload]       NVARCHAR(MAX)  NULL,
    [ResponsePayload]      NVARCHAR(MAX)  NULL,
    [HttpStatusCode]       INT            NULL,
    [CalledAt]             DATETIME2(7)   NOT NULL CONSTRAINT [DF_SupplierApiCallLog_CalledAt] DEFAULT (SYSUTCDATETIME()),
    [IsSuccess]            BIT            NOT NULL,
    [DurationMs]           BIGINT         NULL,
    CONSTRAINT [PK_SupplierApiCallLog] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO

CREATE INDEX [IX_SupplierApiCallLog_RunId]
    ON [dbo].[SupplierApiCallLog] ([RunId]);
GO

CREATE TABLE [dbo].[EtlRuns]
(
    [RunId]                BIGINT         NOT NULL IDENTITY(1, 1),
    [StartedAt]            DATETIME2(7)   NOT NULL CONSTRAINT [DF_EtlRuns_StartedAt] DEFAULT (SYSUTCDATETIME()),
    [CompletedAt]          DATETIME2(7)   NULL,
    [TriggerSource]        NVARCHAR(100)  NOT NULL,
    [Status]               NVARCHAR(30)   NOT NULL,
    [TotalRecords]         INT            NOT NULL CONSTRAINT [DF_EtlRuns_TotalRecords] DEFAULT (0),
    [ValidRecords]         INT            NOT NULL CONSTRAINT [DF_EtlRuns_ValidRecords] DEFAULT (0),
    [InvalidRecords]       INT            NOT NULL CONSTRAINT [DF_EtlRuns_InvalidRecords] DEFAULT (0),
    [NewRecords]           INT            NOT NULL CONSTRAINT [DF_EtlRuns_NewRecords] DEFAULT (0),
    [UpdatedRecords]       INT            NOT NULL CONSTRAINT [DF_EtlRuns_UpdatedRecords] DEFAULT (0),
    [UnchangedRecords]     INT            NOT NULL CONSTRAINT [DF_EtlRuns_UnchangedRecords] DEFAULT (0),
    [SentToApi]            INT            NOT NULL CONSTRAINT [DF_EtlRuns_SentToApi] DEFAULT (0),
    [ApiFailures]          INT            NOT NULL CONSTRAINT [DF_EtlRuns_ApiFailures] DEFAULT (0),
    [RetryCount]           INT            NOT NULL CONSTRAINT [DF_EtlRuns_RetryCount] DEFAULT (0),
    [ErrorRate]            DECIMAL(5,4)   NULL,
    [P95LatencyMs]         BIGINT         NULL,
    [TotalDurationMs]      BIGINT         NULL,
    [SlaCompliantCount]    INT            NULL,
    [FailedBatches]        INT            NOT NULL CONSTRAINT [DF_EtlRuns_FailedBatches] DEFAULT (0),
    CONSTRAINT [PK_EtlRuns] PRIMARY KEY CLUSTERED ([RunId] ASC)
);
GO

CREATE INDEX [IX_EtlRuns_StartedAt]
    ON [dbo].[EtlRuns] ([StartedAt] DESC);
GO

CREATE TABLE [dbo].[SupplierValidationErrors]
(
    [Id]                   BIGINT         NOT NULL IDENTITY(1, 1),
    [RunId]                BIGINT         NOT NULL,
    [RawSupplierId]        NVARCHAR(100)  NULL,
    [ErrorReason]          NVARCHAR(500)  NOT NULL,
    [OccurredAt]           DATETIME2(7)   NOT NULL CONSTRAINT [DF_SupplierValidationErrors_OccurredAt] DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_SupplierValidationErrors] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO

CREATE INDEX [IX_SupplierValidationErrors_RunId]
    ON [dbo].[SupplierValidationErrors] ([RunId]);
GO

CREATE TABLE [dbo].[SupplierPendingRetry]
(
    [Id]                   BIGINT         NOT NULL IDENTITY(1, 1),
    [SupplierId]           NVARCHAR(100)  NOT NULL,
    [OriginalRunId]        BIGINT         NOT NULL,
    [FailedAt]             DATETIME2(7)   NOT NULL CONSTRAINT [DF_SupplierPendingRetry_FailedAt] DEFAULT (SYSUTCDATETIME()),
    [RetryCount]           INT            NOT NULL CONSTRAINT [DF_SupplierPendingRetry_RetryCount] DEFAULT (1),
    [LastErrorMessage]     NVARCHAR(1000) NULL,
    CONSTRAINT [PK_SupplierPendingRetry] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO

CREATE UNIQUE INDEX [UX_SupplierPendingRetry_SupplierId]
    ON [dbo].[SupplierPendingRetry] ([SupplierId]);
GO
