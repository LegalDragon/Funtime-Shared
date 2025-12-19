-- Create FTPBAuth Database
-- Run this script as a user with CREATE DATABASE permissions

USE master;
GO

-- Create database if it doesn't exist
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'FTPBAuth')
BEGIN
    CREATE DATABASE FTPBAuth;
END
GO

USE FTPBAuth;
GO

PRINT 'Database FTPBAuth created successfully';
GO
