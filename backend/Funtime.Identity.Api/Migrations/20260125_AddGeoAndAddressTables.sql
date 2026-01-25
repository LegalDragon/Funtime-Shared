-- =============================================
-- Geo and Address Tables for Funtime Identity
-- =============================================

-- Countries table (import data separately)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Countries')
BEGIN
    CREATE TABLE [dbo].[Countries](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [Name] [nvarchar](100) NOT NULL,
        [Code2] [char](2) NOT NULL,
        [Code3] [char](3) NOT NULL,
        [NumericCode] [char](3) NULL,
        [PhoneCode] [nvarchar](10) NULL,
        [IsActive] [bit] NOT NULL DEFAULT ((1)),
        [SortOrder] [int] NOT NULL DEFAULT ((0)),
        [CreatedAt] [datetime2](7) NOT NULL DEFAULT (GETUTCDATE()),
        PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    CREATE UNIQUE INDEX IX_Countries_Code2 ON Countries(Code2);
    CREATE UNIQUE INDEX IX_Countries_Code3 ON Countries(Code3);
    CREATE INDEX IX_Countries_Active ON Countries(IsActive, SortOrder, Name);
END
GO

-- ProvinceStates table (import data separately)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProvinceStates')
BEGIN
    CREATE TABLE [dbo].[ProvinceStates](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [CountryId] [int] NOT NULL,
        [Name] [nvarchar](100) NOT NULL,
        [Code] [nvarchar](10) NOT NULL,
        [Type] [nvarchar](50) NULL,
        [IsActive] [bit] NOT NULL DEFAULT ((1)),
        [SortOrder] [int] NOT NULL DEFAULT ((0)),
        [CreatedAt] [datetime2](7) NOT NULL DEFAULT (GETUTCDATE()),
        PRIMARY KEY CLUSTERED ([Id] ASC),
        FOREIGN KEY ([CountryId]) REFERENCES [Countries]([Id])
    );

    CREATE INDEX IX_ProvinceStates_Country ON ProvinceStates(CountryId, IsActive, SortOrder, Name);
    CREATE UNIQUE INDEX IX_ProvinceStates_CountryCode ON ProvinceStates(CountryId, Code);
END
GO

-- Cities table (user-populated via UI)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Cities')
BEGIN
    CREATE TABLE [dbo].[Cities](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [ProvinceStateId] [int] NOT NULL,
        [Name] [nvarchar](200) NOT NULL,
        [Latitude] [decimal](9,6) NULL,
        [Longitude] [decimal](9,6) NULL,
        [IsActive] [bit] NOT NULL DEFAULT ((1)),
        [CreatedAt] [datetime2](7) NOT NULL DEFAULT (GETUTCDATE()),
        [CreatedByUserId] [int] NULL,
        PRIMARY KEY CLUSTERED ([Id] ASC),
        FOREIGN KEY ([ProvinceStateId]) REFERENCES [ProvinceStates]([Id])
    );

    CREATE INDEX IX_Cities_Province ON Cities(ProvinceStateId, IsActive, Name);
    CREATE INDEX IX_Cities_Name ON Cities(Name, IsActive);
    CREATE INDEX IX_Cities_GPS ON Cities(Latitude, Longitude) WHERE Latitude IS NOT NULL;
END
GO

-- Addresses table (standalone location registry)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Addresses')
BEGIN
    CREATE TABLE [dbo].[Addresses](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [CityId] [int] NOT NULL,
        [Line1] [nvarchar](200) NOT NULL,
        [Line2] [nvarchar](200) NULL,
        [PostalCode] [nvarchar](20) NULL,
        [Latitude] [decimal](9,6) NULL,
        [Longitude] [decimal](9,6) NULL,
        [IsVerified] [bit] NOT NULL DEFAULT ((0)),
        [CreatedAt] [datetime2](7) NOT NULL DEFAULT (GETUTCDATE()),
        [UpdatedAt] [datetime2](7) NULL,
        [CreatedByUserId] [int] NULL,
        PRIMARY KEY CLUSTERED ([Id] ASC),
        FOREIGN KEY ([CityId]) REFERENCES [Cities]([Id])
    );

    -- Index for duplicate detection
    CREATE INDEX IX_Addresses_Lookup ON Addresses(CityId, Line1, PostalCode);

    -- Index for GPS queries
    CREATE INDEX IX_Addresses_GPS ON Addresses(Latitude, Longitude) WHERE Latitude IS NOT NULL;
END
GO

-- =============================================
-- Helper function for distance calculation
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[fn_CalculateDistance]') AND type = N'FN')
BEGIN
    EXEC('
    CREATE FUNCTION [dbo].[fn_CalculateDistance]
    (
        @lat1 DECIMAL(9,6),
        @lng1 DECIMAL(9,6),
        @lat2 DECIMAL(9,6),
        @lng2 DECIMAL(9,6)
    )
    RETURNS FLOAT
    AS
    BEGIN
        -- Haversine formula - returns distance in kilometers
        IF @lat1 IS NULL OR @lng1 IS NULL OR @lat2 IS NULL OR @lng2 IS NULL
            RETURN NULL;

        DECLARE @R FLOAT = 6371; -- Earth radius in km
        DECLARE @dLat FLOAT = RADIANS(@lat2 - @lat1);
        DECLARE @dLng FLOAT = RADIANS(@lng2 - @lng1);
        DECLARE @a FLOAT = SIN(@dLat/2) * SIN(@dLat/2) +
                          COS(RADIANS(@lat1)) * COS(RADIANS(@lat2)) *
                          SIN(@dLng/2) * SIN(@dLng/2);
        DECLARE @c FLOAT = 2 * ATN2(SQRT(@a), SQRT(1-@a));

        RETURN @R * @c;
    END
    ');
END
GO
