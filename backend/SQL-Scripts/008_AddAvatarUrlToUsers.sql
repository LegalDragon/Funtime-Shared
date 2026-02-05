-- Migration: Add AvatarUrl column to Users table
-- Date: 2026-02-05
-- Description: Store profile picture URL from OAuth providers

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'AvatarUrl'
)
BEGIN
    ALTER TABLE Users ADD AvatarUrl NVARCHAR(500) NULL;
    PRINT 'Added AvatarUrl column to Users table';
END
ELSE
BEGIN
    PRINT 'AvatarUrl column already exists';
END
GO
