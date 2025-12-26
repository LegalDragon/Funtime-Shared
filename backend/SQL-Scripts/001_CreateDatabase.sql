-- Create FuntimeIdentity Database
-- Run this script as a user with CREATE DATABASE permissions

USE master;
GO

-- Create database if it doesn't exist
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'FuntimeIdentity')
BEGIN
    CREATE DATABASE FuntimeIdentity;
END
GO

USE FuntimeIdentity;
GO

PRINT 'Database FuntimeIdentity created successfully';
GO
