-- Migration: Add ApiKeys table for multi-partner API key authentication
-- Date: 2025-01-17

-- =====================================================
-- Create ApiKeys table
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ApiKeys')
BEGIN
    CREATE TABLE ApiKeys (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        PartnerKey NVARCHAR(50) NOT NULL,           -- Unique identifier for the partner (e.g., 'community', 'college')
        PartnerName NVARCHAR(100) NOT NULL,         -- Display name (e.g., 'Pickleball Community')
        ApiKey NVARCHAR(64) NOT NULL,               -- The actual API key (should be unique)
        KeyPrefix NVARCHAR(10) NOT NULL,            -- First 8 chars for identification (e.g., 'pk_live_')
        Scopes NVARCHAR(MAX) NULL,                  -- JSON array of allowed scopes
        AllowedIPs NVARCHAR(MAX) NULL,              -- JSON array of allowed IPs/CIDRs (null = any)
        AllowedOrigins NVARCHAR(MAX) NULL,          -- JSON array of allowed origins for CORS
        RateLimitPerMinute INT NOT NULL DEFAULT 60,
        IsActive BIT NOT NULL DEFAULT 1,
        ExpiresAt DATETIME2 NULL,                   -- Null = never expires
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NULL,
        LastUsedAt DATETIME2 NULL,
        UsageCount BIGINT NOT NULL DEFAULT 0,
        Description NVARCHAR(500) NULL,
        CreatedBy NVARCHAR(100) NULL,

        CONSTRAINT UQ_ApiKeys_ApiKey UNIQUE (ApiKey),
        CONSTRAINT UQ_ApiKeys_PartnerKey UNIQUE (PartnerKey)
    );

    CREATE INDEX IX_ApiKeys_ApiKey ON ApiKeys(ApiKey) WHERE IsActive = 1;
    CREATE INDEX IX_ApiKeys_PartnerKey ON ApiKeys(PartnerKey) WHERE IsActive = 1;

    PRINT 'Created ApiKeys table';
END
GO

-- =====================================================
-- Stored Procedures for API Key Management
-- =====================================================

-- Get API key by key value (for validation)
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'csp_ApiKey_GetByKey')
    DROP PROCEDURE csp_ApiKey_GetByKey;
GO

CREATE PROCEDURE csp_ApiKey_GetByKey
    @ApiKey NVARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        Id, PartnerKey, PartnerName, ApiKey, KeyPrefix,
        Scopes, AllowedIPs, AllowedOrigins, RateLimitPerMinute,
        IsActive, ExpiresAt, CreatedAt, UpdatedAt, LastUsedAt,
        UsageCount, Description, CreatedBy
    FROM ApiKeys
    WHERE ApiKey = @ApiKey AND IsActive = 1
      AND (ExpiresAt IS NULL OR ExpiresAt > GETUTCDATE());
END
GO

-- Get all API keys (for admin)
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'csp_ApiKey_GetAll')
    DROP PROCEDURE csp_ApiKey_GetAll;
GO

CREATE PROCEDURE csp_ApiKey_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        Id, PartnerKey, PartnerName,
        KeyPrefix + '...' AS ApiKeyMasked,  -- Don't return full key
        KeyPrefix, Scopes, AllowedIPs, AllowedOrigins,
        RateLimitPerMinute, IsActive, ExpiresAt,
        CreatedAt, UpdatedAt, LastUsedAt, UsageCount,
        Description, CreatedBy
    FROM ApiKeys
    ORDER BY CreatedAt DESC;
END
GO

-- Create new API key
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'csp_ApiKey_Create')
    DROP PROCEDURE csp_ApiKey_Create;
GO

CREATE PROCEDURE csp_ApiKey_Create
    @PartnerKey NVARCHAR(50),
    @PartnerName NVARCHAR(100),
    @ApiKey NVARCHAR(64),
    @KeyPrefix NVARCHAR(10),
    @Scopes NVARCHAR(MAX) = NULL,
    @AllowedIPs NVARCHAR(MAX) = NULL,
    @AllowedOrigins NVARCHAR(MAX) = NULL,
    @RateLimitPerMinute INT = 60,
    @ExpiresAt DATETIME2 = NULL,
    @Description NVARCHAR(500) = NULL,
    @CreatedBy NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Check for duplicate partner key
    IF EXISTS (SELECT 1 FROM ApiKeys WHERE PartnerKey = @PartnerKey)
    BEGIN
        RAISERROR('Partner key already exists', 16, 1);
        RETURN;
    END

    INSERT INTO ApiKeys (
        PartnerKey, PartnerName, ApiKey, KeyPrefix,
        Scopes, AllowedIPs, AllowedOrigins, RateLimitPerMinute,
        ExpiresAt, Description, CreatedBy
    )
    VALUES (
        @PartnerKey, @PartnerName, @ApiKey, @KeyPrefix,
        @Scopes, @AllowedIPs, @AllowedOrigins, @RateLimitPerMinute,
        @ExpiresAt, @Description, @CreatedBy
    );

    -- Return the created record (with full key for initial display)
    SELECT
        Id, PartnerKey, PartnerName, ApiKey, KeyPrefix,
        Scopes, AllowedIPs, AllowedOrigins, RateLimitPerMinute,
        IsActive, ExpiresAt, CreatedAt, UpdatedAt, LastUsedAt,
        UsageCount, Description, CreatedBy
    FROM ApiKeys
    WHERE Id = SCOPE_IDENTITY();
END
GO

-- Update API key
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'csp_ApiKey_Update')
    DROP PROCEDURE csp_ApiKey_Update;
GO

CREATE PROCEDURE csp_ApiKey_Update
    @Id INT,
    @PartnerName NVARCHAR(100) = NULL,
    @Scopes NVARCHAR(MAX) = NULL,
    @AllowedIPs NVARCHAR(MAX) = NULL,
    @AllowedOrigins NVARCHAR(MAX) = NULL,
    @RateLimitPerMinute INT = NULL,
    @IsActive BIT = NULL,
    @ExpiresAt DATETIME2 = NULL,
    @Description NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE ApiKeys
    SET
        PartnerName = ISNULL(@PartnerName, PartnerName),
        Scopes = CASE WHEN @Scopes IS NOT NULL THEN @Scopes ELSE Scopes END,
        AllowedIPs = CASE WHEN @AllowedIPs IS NOT NULL THEN @AllowedIPs ELSE AllowedIPs END,
        AllowedOrigins = CASE WHEN @AllowedOrigins IS NOT NULL THEN @AllowedOrigins ELSE AllowedOrigins END,
        RateLimitPerMinute = ISNULL(@RateLimitPerMinute, RateLimitPerMinute),
        IsActive = ISNULL(@IsActive, IsActive),
        ExpiresAt = CASE WHEN @ExpiresAt IS NOT NULL THEN @ExpiresAt ELSE ExpiresAt END,
        Description = CASE WHEN @Description IS NOT NULL THEN @Description ELSE Description END,
        UpdatedAt = GETUTCDATE()
    WHERE Id = @Id;

    SELECT
        Id, PartnerKey, PartnerName,
        KeyPrefix + '...' AS ApiKeyMasked,
        KeyPrefix, Scopes, AllowedIPs, AllowedOrigins,
        RateLimitPerMinute, IsActive, ExpiresAt,
        CreatedAt, UpdatedAt, LastUsedAt, UsageCount,
        Description, CreatedBy
    FROM ApiKeys
    WHERE Id = @Id;
END
GO

-- Delete API key
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'csp_ApiKey_Delete')
    DROP PROCEDURE csp_ApiKey_Delete;
GO

CREATE PROCEDURE csp_ApiKey_Delete
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM ApiKeys WHERE Id = @Id;

    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- Update last used timestamp and increment usage count
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'csp_ApiKey_RecordUsage')
    DROP PROCEDURE csp_ApiKey_RecordUsage;
GO

CREATE PROCEDURE csp_ApiKey_RecordUsage
    @ApiKey NVARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE ApiKeys
    SET LastUsedAt = GETUTCDATE(),
        UsageCount = UsageCount + 1
    WHERE ApiKey = @ApiKey;
END
GO

-- Regenerate API key (creates new key for existing partner)
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'csp_ApiKey_Regenerate')
    DROP PROCEDURE csp_ApiKey_Regenerate;
GO

CREATE PROCEDURE csp_ApiKey_Regenerate
    @Id INT,
    @NewApiKey NVARCHAR(64),
    @NewKeyPrefix NVARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE ApiKeys
    SET ApiKey = @NewApiKey,
        KeyPrefix = @NewKeyPrefix,
        UpdatedAt = GETUTCDATE(),
        UsageCount = 0,
        LastUsedAt = NULL
    WHERE Id = @Id;

    -- Return the updated record with full new key
    SELECT
        Id, PartnerKey, PartnerName, ApiKey, KeyPrefix,
        Scopes, AllowedIPs, AllowedOrigins, RateLimitPerMinute,
        IsActive, ExpiresAt, CreatedAt, UpdatedAt, LastUsedAt,
        UsageCount, Description, CreatedBy
    FROM ApiKeys
    WHERE Id = @Id;
END
GO

-- =====================================================
-- Seed default API keys (optional)
-- =====================================================
-- Uncomment to create initial keys for your sites

/*
EXEC csp_ApiKey_Create
    @PartnerKey = 'community',
    @PartnerName = 'Pickleball Community',
    @ApiKey = 'pk_community_CHANGE_THIS_TO_RANDOM_KEY_1234567890',
    @KeyPrefix = 'pk_comm_',
    @Scopes = '["auth:validate", "users:read", "assets:read", "assets:write"]',
    @Description = 'API key for Pickleball Community site',
    @CreatedBy = 'system';

EXEC csp_ApiKey_Create
    @PartnerKey = 'college',
    @PartnerName = 'Pickleball College',
    @ApiKey = 'pk_college_CHANGE_THIS_TO_RANDOM_KEY_0987654321',
    @KeyPrefix = 'pk_coll_',
    @Scopes = '["auth:validate", "users:read", "assets:read", "assets:write"]',
    @Description = 'API key for Pickleball College site',
    @CreatedBy = 'system';
*/

PRINT 'API Keys migration completed successfully';
GO
