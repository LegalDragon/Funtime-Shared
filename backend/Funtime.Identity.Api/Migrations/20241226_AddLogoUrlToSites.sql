-- Migration: Add LogoUrl column to Sites table
-- Date: 2024-12-26

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Sites') AND name = 'LogoUrl')
BEGIN
    ALTER TABLE Sites ADD LogoUrl NVARCHAR(500) NULL;
END
GO
