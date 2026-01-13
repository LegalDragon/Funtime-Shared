-- Migration: Add AssetType and ExternalUrl columns to Assets table
-- Date: 2024-12-28

-- Add AssetType column (image, video, document, audio, link)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Assets') AND name = 'AssetType')
BEGIN
    ALTER TABLE Assets ADD AssetType NVARCHAR(20) NOT NULL DEFAULT 'image';
END
GO

-- Add ExternalUrl column for YouTube, Vimeo, etc.
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Assets') AND name = 'ExternalUrl')
BEGIN
    ALTER TABLE Assets ADD ExternalUrl NVARCHAR(2000) NULL;
END
GO

-- Add ThumbnailUrl for video thumbnails
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Assets') AND name = 'ThumbnailUrl')
BEGIN
    ALTER TABLE Assets ADD ThumbnailUrl NVARCHAR(1000) NULL;
END
GO

-- Add index on AssetType
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Assets_AssetType')
BEGIN
    CREATE INDEX IX_Assets_AssetType ON Assets(AssetType);
END
GO
