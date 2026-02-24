CREATE TABLE [dbo].[EtlJob]
(
    [Id]          INT            NOT NULL IDENTITY(1, 1),
    [Name]        NVARCHAR(200)  NOT NULL,
    [Description] NVARCHAR(1000) NULL,
    [CronSchedule] NVARCHAR(100) NOT NULL,
    [IsEnabled]   BIT            NOT NULL CONSTRAINT [DF_EtlJob_IsEnabled] DEFAULT (1),
    [CreatedAt]   DATETIME2(7)   NOT NULL CONSTRAINT [DF_EtlJob_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt]   DATETIME2(7)   NOT NULL CONSTRAINT [DF_EtlJob_UpdatedAt] DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_EtlJob] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO

CREATE UNIQUE INDEX [UX_EtlJob_Name] ON [dbo].[EtlJob] ([Name]);
GO
