CREATE TABLE [dbo].[SupplierEtlRun]
(
    [Id]                    BIGINT         NOT NULL IDENTITY(1, 1),
    [TriggerSource]         NVARCHAR(100)  NOT NULL,
    [CorrelationId]         NVARCHAR(100)  NULL,
    [Status]                NVARCHAR(50)   NOT NULL CONSTRAINT [DF_SupplierEtlRun_Status] DEFAULT ('Running'),
    [StartedAt]             DATETIME2(7)   NOT NULL CONSTRAINT [DF_SupplierEtlRun_StartedAt] DEFAULT (SYSUTCDATETIME()),
    [FinishedAt]            DATETIME2(7)   NULL,
    [RecordsIn]             INT            NOT NULL CONSTRAINT [DF_SupplierEtlRun_RecordsIn] DEFAULT (0),
    [RecordsValidated]      INT            NOT NULL CONSTRAINT [DF_SupplierEtlRun_RecordsValidated] DEFAULT (0),
    [RecordsSent]           INT            NOT NULL CONSTRAINT [DF_SupplierEtlRun_RecordsSent] DEFAULT (0),
    [RecordsFailed]         INT            NOT NULL CONSTRAINT [DF_SupplierEtlRun_RecordsFailed] DEFAULT (0),
    [RecordsSkipped]        INT            NOT NULL CONSTRAINT [DF_SupplierEtlRun_RecordsSkipped] DEFAULT (0),
    [ValidationFailureCount] INT           NOT NULL CONSTRAINT [DF_SupplierEtlRun_ValidationFailureCount] DEFAULT (0),
    [ApiFailureCount]       INT            NOT NULL CONSTRAINT [DF_SupplierEtlRun_ApiFailureCount] DEFAULT (0),
    [P95LatencyMs]          INT            NULL,
    [SlaCompliancePct]      DECIMAL(5, 2)  NULL,
    [FailedBatchesCount]    INT            NOT NULL CONSTRAINT [DF_SupplierEtlRun_FailedBatchesCount] DEFAULT (0),
    [TotalProcessingMs]     INT            NULL,
    [RetryCount]            INT            NOT NULL CONSTRAINT [DF_SupplierEtlRun_RetryCount] DEFAULT (0),
    [ErrorMessage]          NVARCHAR(MAX)  NULL,
    [CreatedAt]             DATETIME2(7)   NOT NULL CONSTRAINT [DF_SupplierEtlRun_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_SupplierEtlRun] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO

CREATE INDEX [IX_SupplierEtlRun_StartedAt] ON [dbo].[SupplierEtlRun] ([StartedAt] DESC);
GO
CREATE INDEX [IX_SupplierEtlRun_Status] ON [dbo].[SupplierEtlRun] ([Status]);
GO
CREATE INDEX [IX_SupplierEtlRun_CorrelationId] ON [dbo].[SupplierEtlRun] ([CorrelationId]);
GO
