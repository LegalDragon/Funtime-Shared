-- Migration: Add AssetFileTypes table for configurable file type settings
-- Date: 2025-01-16

-- Create AssetFileTypes table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AssetFileTypes')
BEGIN
    CREATE TABLE AssetFileTypes (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        MimeType NVARCHAR(100) NOT NULL,
        Extensions NVARCHAR(100) NOT NULL,        -- Comma-separated extensions (e.g., ".jpg,.jpeg")
        Category NVARCHAR(20) NOT NULL,           -- image, video, audio, document
        MaxSizeMB INT NOT NULL DEFAULT 10,
        IsEnabled BIT NOT NULL DEFAULT 1,
        DisplayName NVARCHAR(50) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NULL,
        CONSTRAINT UQ_AssetFileTypes_MimeType UNIQUE (MimeType)
    );

    -- Indexes
    CREATE INDEX IX_AssetFileTypes_Category ON AssetFileTypes(Category);
    CREATE INDEX IX_AssetFileTypes_IsEnabled ON AssetFileTypes(IsEnabled);
END
GO

-- Seed initial file types (current hardcoded values)
IF NOT EXISTS (SELECT 1 FROM AssetFileTypes)
BEGIN
    -- Images
    INSERT INTO AssetFileTypes (MimeType, Extensions, Category, MaxSizeMB, DisplayName) VALUES
    ('image/jpeg', '.jpg,.jpeg', 'image', 10, 'JPEG Image'),
    ('image/png', '.png', 'image', 10, 'PNG Image'),
    ('image/gif', '.gif', 'image', 10, 'GIF Image'),
    ('image/webp', '.webp', 'image', 10, 'WebP Image'),
    ('image/svg+xml', '.svg', 'image', 10, 'SVG Image');

    -- Videos
    INSERT INTO AssetFileTypes (MimeType, Extensions, Category, MaxSizeMB, DisplayName) VALUES
    ('video/mp4', '.mp4', 'video', 100, 'MP4 Video'),
    ('video/webm', '.webm', 'video', 100, 'WebM Video'),
    ('video/ogg', '.ogg,.ogv', 'video', 100, 'OGG Video'),
    ('video/quicktime', '.mov', 'video', 100, 'QuickTime Video'),
    ('video/x-msvideo', '.avi', 'video', 100, 'AVI Video'),
    ('video/avi', '.avi', 'video', 100, 'AVI Video (alt)'),
    ('video/x-matroska', '.mkv', 'video', 100, 'MKV Video'),
    ('video/x-m4v', '.m4v', 'video', 100, 'M4V Video'),
    ('video/m4v', '.m4v', 'video', 100, 'M4V Video (alt)'),
    ('video/mpeg', '.mpeg,.mpg', 'video', 100, 'MPEG Video'),
    ('video/x-ms-wmv', '.wmv', 'video', 100, 'WMV Video'),
    ('video/3gpp', '.3gp', 'video', 100, '3GP Video'),
    ('video/3gpp2', '.3g2', 'video', 100, '3GP2 Video');

    -- Audio
    INSERT INTO AssetFileTypes (MimeType, Extensions, Category, MaxSizeMB, DisplayName) VALUES
    ('audio/mpeg', '.mp3', 'audio', 10, 'MP3 Audio'),
    ('audio/mp3', '.mp3', 'audio', 10, 'MP3 Audio (alt)'),
    ('audio/wav', '.wav', 'audio', 10, 'WAV Audio'),
    ('audio/ogg', '.ogg', 'audio', 10, 'OGG Audio');

    -- Documents
    INSERT INTO AssetFileTypes (MimeType, Extensions, Category, MaxSizeMB, DisplayName) VALUES
    ('application/pdf', '.pdf', 'document', 10, 'PDF Document'),
    ('application/msword', '.doc', 'document', 10, 'Word Document'),
    ('application/vnd.openxmlformats-officedocument.wordprocessingml.document', '.docx', 'document', 10, 'Word Document (DOCX)'),
    ('text/markdown', '.md', 'document', 10, 'Markdown'),
    ('text/x-markdown', '.md', 'document', 10, 'Markdown (alt)'),
    ('text/html', '.html,.htm', 'document', 10, 'HTML Document');
END
GO

-- Stored Procedure: Get all enabled file types
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'csp_AssetFileTypes_GetEnabled')
    DROP PROCEDURE csp_AssetFileTypes_GetEnabled;
GO

CREATE PROCEDURE csp_AssetFileTypes_GetEnabled
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, MimeType, Extensions, Category, MaxSizeMB, IsEnabled, DisplayName, CreatedAt, UpdatedAt
    FROM AssetFileTypes
    WHERE IsEnabled = 1
    ORDER BY Category, DisplayName;
END
GO

-- Stored Procedure: Get all file types (for admin)
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'csp_AssetFileTypes_GetAll')
    DROP PROCEDURE csp_AssetFileTypes_GetAll;
GO

CREATE PROCEDURE csp_AssetFileTypes_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, MimeType, Extensions, Category, MaxSizeMB, IsEnabled, DisplayName, CreatedAt, UpdatedAt
    FROM AssetFileTypes
    ORDER BY Category, DisplayName;
END
GO

-- Stored Procedure: Get file type by ID
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'csp_AssetFileTypes_GetById')
    DROP PROCEDURE csp_AssetFileTypes_GetById;
GO

CREATE PROCEDURE csp_AssetFileTypes_GetById
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, MimeType, Extensions, Category, MaxSizeMB, IsEnabled, DisplayName, CreatedAt, UpdatedAt
    FROM AssetFileTypes
    WHERE Id = @Id;
END
GO

-- Stored Procedure: Create file type
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'csp_AssetFileTypes_Create')
    DROP PROCEDURE csp_AssetFileTypes_Create;
GO

CREATE PROCEDURE csp_AssetFileTypes_Create
    @MimeType NVARCHAR(100),
    @Extensions NVARCHAR(100),
    @Category NVARCHAR(20),
    @MaxSizeMB INT = 10,
    @IsEnabled BIT = 1,
    @DisplayName NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Check if MimeType already exists
    IF EXISTS (SELECT 1 FROM AssetFileTypes WHERE MimeType = @MimeType)
    BEGIN
        RAISERROR('MimeType already exists', 16, 1);
        RETURN;
    END

    INSERT INTO AssetFileTypes (MimeType, Extensions, Category, MaxSizeMB, IsEnabled, DisplayName)
    VALUES (@MimeType, @Extensions, @Category, @MaxSizeMB, @IsEnabled, @DisplayName);

    SELECT SCOPE_IDENTITY() AS Id;
END
GO

-- Stored Procedure: Update file type
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'csp_AssetFileTypes_Update')
    DROP PROCEDURE csp_AssetFileTypes_Update;
GO

CREATE PROCEDURE csp_AssetFileTypes_Update
    @Id INT,
    @MimeType NVARCHAR(100),
    @Extensions NVARCHAR(100),
    @Category NVARCHAR(20),
    @MaxSizeMB INT,
    @IsEnabled BIT,
    @DisplayName NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Check if MimeType already exists for another record
    IF EXISTS (SELECT 1 FROM AssetFileTypes WHERE MimeType = @MimeType AND Id != @Id)
    BEGIN
        RAISERROR('MimeType already exists', 16, 1);
        RETURN;
    END

    UPDATE AssetFileTypes
    SET MimeType = @MimeType,
        Extensions = @Extensions,
        Category = @Category,
        MaxSizeMB = @MaxSizeMB,
        IsEnabled = @IsEnabled,
        DisplayName = @DisplayName,
        UpdatedAt = GETUTCDATE()
    WHERE Id = @Id;

    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- Stored Procedure: Delete file type
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'csp_AssetFileTypes_Delete')
    DROP PROCEDURE csp_AssetFileTypes_Delete;
GO

CREATE PROCEDURE csp_AssetFileTypes_Delete
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM AssetFileTypes WHERE Id = @Id;

    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- Stored Procedure: Toggle file type enabled status
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'csp_AssetFileTypes_ToggleEnabled')
    DROP PROCEDURE csp_AssetFileTypes_ToggleEnabled;
GO

CREATE PROCEDURE csp_AssetFileTypes_ToggleEnabled
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE AssetFileTypes
    SET IsEnabled = CASE WHEN IsEnabled = 1 THEN 0 ELSE 1 END,
        UpdatedAt = GETUTCDATE()
    WHERE Id = @Id;

    SELECT IsEnabled FROM AssetFileTypes WHERE Id = @Id;
END
GO

-- Stored Procedure: Get file types by category
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'csp_AssetFileTypes_GetByCategory')
    DROP PROCEDURE csp_AssetFileTypes_GetByCategory;
GO

CREATE PROCEDURE csp_AssetFileTypes_GetByCategory
    @Category NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, MimeType, Extensions, Category, MaxSizeMB, IsEnabled, DisplayName, CreatedAt, UpdatedAt
    FROM AssetFileTypes
    WHERE Category = @Category AND IsEnabled = 1
    ORDER BY DisplayName;
END
GO
