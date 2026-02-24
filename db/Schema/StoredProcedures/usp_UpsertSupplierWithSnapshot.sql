CREATE PROCEDURE [dbo].[usp_UpsertSupplierWithSnapshot]
    @EtlRunId             BIGINT,
    @SupplierId           NVARCHAR(100),
    @SupplierName         NVARCHAR(300) = NULL,
    @BankAccountName      NVARCHAR(200) = NULL,
    @BankAccountNumber    NVARCHAR(100) = NULL,
    @BankRoutingNumber    NVARCHAR(100) = NULL,
    @BankCountryCode      NVARCHAR(10)  = NULL,
    @AddressLine1         NVARCHAR(300) = NULL,
    @AddressLine2         NVARCHAR(300) = NULL,
    @City                 NVARCHAR(100) = NULL,
    @StateProvince        NVARCHAR(100) = NULL,
    @PostalCode           NVARCHAR(30)  = NULL,
    @CountryCode          NVARCHAR(10)  = NULL,
    @SnapshotHash         NVARCHAR(128) = NULL,
    @SnapshotPayload      NVARCHAR(MAX),
    @ChangeType           NVARCHAR(50) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @SupplierPk BIGINT;
    DECLARE @ExistingHash NVARCHAR(128);

    SELECT @SupplierPk = s.[Id],
           @ExistingHash = s.[LastSnapshotHash]
    FROM [dbo].[Supplier] AS s
    WHERE s.[SupplierId] = @SupplierId;

    IF @SupplierPk IS NULL
    BEGIN
        INSERT INTO [dbo].[Supplier]
        (
            [SupplierId],
            [SupplierName],
            [BankAccountName],
            [BankAccountNumber],
            [BankRoutingNumber],
            [BankCountryCode],
            [AddressLine1],
            [AddressLine2],
            [City],
            [StateProvince],
            [PostalCode],
            [CountryCode],
            [LastSnapshotHash],
            [DeliveryStatus],
            [LastSeenRunId]
        )
        VALUES
        (
            @SupplierId,
            @SupplierName,
            @BankAccountName,
            @BankAccountNumber,
            @BankRoutingNumber,
            @BankCountryCode,
            @AddressLine1,
            @AddressLine2,
            @City,
            @StateProvince,
            @PostalCode,
            @CountryCode,
            @SnapshotHash,
            'NOT_SENT',
            @EtlRunId
        );

        SET @SupplierPk = SCOPE_IDENTITY();
        SET @ChangeType = 'NEW';
    END
    ELSE
    BEGIN
        IF (ISNULL(@ExistingHash, '') <> ISNULL(@SnapshotHash, ''))
        BEGIN
            UPDATE [dbo].[Supplier]
            SET    [SupplierName]      = @SupplierName,
                   [BankAccountName]   = @BankAccountName,
                   [BankAccountNumber] = @BankAccountNumber,
                   [BankRoutingNumber] = @BankRoutingNumber,
                   [BankCountryCode]   = @BankCountryCode,
                   [AddressLine1]      = @AddressLine1,
                   [AddressLine2]      = @AddressLine2,
                   [City]              = @City,
                   [StateProvince]     = @StateProvince,
                   [PostalCode]        = @PostalCode,
                   [CountryCode]       = @CountryCode,
                   [LastSnapshotHash]  = @SnapshotHash,
                   [UpdatedAt]         = SYSUTCDATETIME(),
                   [LastSeenRunId]     = @EtlRunId
            WHERE  [Id] = @SupplierPk;

            SET @ChangeType = 'UPDATED';
        END
        ELSE
        BEGIN
            UPDATE [dbo].[Supplier]
            SET    [LastSeenRunId] = @EtlRunId,
                   [UpdatedAt] = SYSUTCDATETIME()
            WHERE  [Id] = @SupplierPk;

            SET @ChangeType = 'UNCHANGED';
        END
    END;

    IF (@ChangeType IN ('NEW', 'UPDATED'))
    BEGIN
        INSERT INTO [dbo].[SupplierSnapshot]
        (
            [SupplierId],
            [EtlRunId],
            [ChangeType],
            [SnapshotHash],
            [SnapshotPayload]
        )
        VALUES
        (
            @SupplierPk,
            @EtlRunId,
            @ChangeType,
            @SnapshotHash,
            @SnapshotPayload
        );
    END;
END;
GO
