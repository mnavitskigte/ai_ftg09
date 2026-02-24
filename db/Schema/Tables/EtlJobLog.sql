CREATE TABLE [dbo].[EtlJobLog]
(
    [Id]          BIGINT         NOT NULL IDENTITY(1, 1),
    [EtlJobId]    INT            NOT NULL,
    [Status]      NVARCHAR(50)   NOT NULL,   -- Pending, Running, Completed, Failed
    [StartedAt]   DATETIME2(7)   NULL,
    [FinishedAt]  DATETIME2(7)   NULL,
    [ErrorMessage] NVARCHAR(MAX) NULL,
    [RowsProcessed] INT          NULL,
    [CreatedAt]   DATETIME2(7)   NOT NULL CONSTRAINT [DF_EtlJobLog_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_EtlJobLog] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_EtlJobLog_EtlJob] FOREIGN KEY ([EtlJobId]) REFERENCES [dbo].[EtlJob] ([Id])
);
GO

CREATE INDEX [IX_EtlJobLog_EtlJobId] ON [dbo].[EtlJobLog] ([EtlJobId]);
GO
CREATE INDEX [IX_EtlJobLog_Status] ON [dbo].[EtlJobLog] ([Status]);
GO
