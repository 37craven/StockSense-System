/*
    StockSense - Motorcycle part compatibility schema

    Purpose:
      Creates the motorcycle compatibility catalogue and its mapping to the
      existing StockSense inventory table.

    Inventory prerequisite:
      dbo.Products(Id INT) must already exist. This script does not create or
      modify the Products table.

    Re-runnable:
      Tables, constraints, and indexes are created only when absent. Existing
      objects and data are preserved.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    ---------------------------------------------------------------------------
    -- Validate the existing inventory contract before creating dependencies.
    ---------------------------------------------------------------------------
    IF OBJECT_ID(N'dbo.Products', N'U') IS NULL
        THROW 50001, 'Prerequisite table dbo.Products does not exist. No changes were made.', 1;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.columns AS c
        WHERE c.object_id = OBJECT_ID(N'dbo.Products', N'U')
          AND c.name = N'Id'
          AND c.system_type_id = TYPE_ID(N'int')
          AND c.user_type_id = TYPE_ID(N'int')
    )
        THROW 50002, 'Prerequisite column dbo.Products.Id must exist and have data type INT. No changes were made.', 1;

    ---------------------------------------------------------------------------
    -- Motorcycle model/version specifications.
    ---------------------------------------------------------------------------
    IF OBJECT_ID(N'dbo.MotorCompatibility', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.MotorCompatibility
        (
            CompatibilityID  INT IDENTITY(1,1) NOT NULL,
            Manufacturer     VARCHAR(50)        NOT NULL,
            ModelName        VARCHAR(100)       NOT NULL,
            VersionName      VARCHAR(50)        NOT NULL,
            YearStart        INT                NOT NULL,
            YearEnd          INT                NULL,
            EngineOilSpec    VARCHAR(100)       NULL,
            GearOilSpec      VARCHAR(100)       NULL,
            CoolantSpec      VARCHAR(100)       NULL,
            SparkPlugSpec    VARCHAR(100)       NULL,
            FuelFilterSpec   VARCHAR(100)       NULL,
            DriveBeltSpec    VARCHAR(100)       NULL,
            FlyBallWeight    VARCHAR(50)        NULL,
            CenterSpringSpec VARCHAR(50)        NULL,
            BrakePadFront    VARCHAR(100)       NULL,
            BrakePadRear     VARCHAR(100)       NULL,
            BrakeShoeRear    VARCHAR(100)       NULL,
            AirFilterSpec    VARCHAR(100)       NULL,

            CONSTRAINT PK_MotorCompatibility
                PRIMARY KEY CLUSTERED (CompatibilityID),
            CONSTRAINT CK_MotorCompatibility_Manufacturer
                CHECK (Manufacturer IN ('Honda', 'Yamaha', 'Suzuki', 'Kawasaki', 'Rusi')),
            CONSTRAINT CK_MotorCompatibility_YearRange
                CHECK (YearStart >= 1885 AND (YearEnd IS NULL OR YearEnd >= YearStart))
        );
    END;

    ---------------------------------------------------------------------------
    -- Junction between compatibility records and existing inventory products.
    -- Deletes are deliberately restricted to prevent accidental loss of mapping
    -- history; remove mappings explicitly before deleting either parent row.
    ---------------------------------------------------------------------------
    IF OBJECT_ID(N'dbo.ProductCompatibilityMapping', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.ProductCompatibilityMapping
        (
            MappingID       INT IDENTITY(1,1) NOT NULL,
            CompatibilityID INT               NOT NULL,
            ProductID       INT               NOT NULL,
            PartFunction    VARCHAR(50)       NOT NULL,
            IsOEM           BIT               NOT NULL
                CONSTRAINT DF_ProductCompatibilityMapping_IsOEM DEFAULT (0),
            Notes           VARCHAR(255)      NULL,

            CONSTRAINT PK_ProductCompatibilityMapping
                PRIMARY KEY CLUSTERED (MappingID),
            CONSTRAINT FK_ProductCompatibilityMapping_MotorCompatibility
                FOREIGN KEY (CompatibilityID)
                REFERENCES dbo.MotorCompatibility (CompatibilityID),
            CONSTRAINT FK_ProductCompatibilityMapping_Products
                FOREIGN KEY (ProductID)
                REFERENCES dbo.Products (Id)
        );
    END;

    ---------------------------------------------------------------------------
    -- Lookup and integrity indexes. The unique index prevents duplicate mappings
    -- for the same motorcycle, inventory item, and part function.
    ---------------------------------------------------------------------------
    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.MotorCompatibility', N'U')
          AND name = N'UX_MotorCompatibility_ModelVersionYears'
    )
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX UX_MotorCompatibility_ModelVersionYears
            ON dbo.MotorCompatibility
               (Manufacturer, ModelName, VersionName, YearStart, YearEnd);
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.ProductCompatibilityMapping', N'U')
          AND name = N'UX_ProductCompatibilityMapping_CompatibilityProductFunction'
    )
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX UX_ProductCompatibilityMapping_CompatibilityProductFunction
            ON dbo.ProductCompatibilityMapping
               (CompatibilityID, ProductID, PartFunction);
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.ProductCompatibilityMapping', N'U')
          AND name = N'IX_ProductCompatibilityMapping_ProductID'
    )
    BEGIN
        CREATE NONCLUSTERED INDEX IX_ProductCompatibilityMapping_ProductID
            ON dbo.ProductCompatibilityMapping (ProductID)
            INCLUDE (CompatibilityID, PartFunction, IsOEM);
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;

