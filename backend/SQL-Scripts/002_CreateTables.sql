-- Create Tables for FTPBAuth
-- Run this script after creating the database

USE FTPBAuth;
GO

-- Users Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Users] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Email] NVARCHAR(255) NULL,
        [PasswordHash] NVARCHAR(255) NULL,
        [PhoneNumber] NVARCHAR(20) NULL,
        [IsEmailVerified] BIT NOT NULL DEFAULT 0,
        [IsPhoneVerified] BIT NOT NULL DEFAULT 0,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] DATETIME2 NULL,
        [LastLoginAt] DATETIME2 NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    -- Unique index on Email (filtered to non-null values)
    CREATE UNIQUE NONCLUSTERED INDEX [IX_Users_Email]
        ON [dbo].[Users] ([Email])
        WHERE [Email] IS NOT NULL;

    -- Unique index on PhoneNumber (filtered to non-null values)
    CREATE UNIQUE NONCLUSTERED INDEX [IX_Users_PhoneNumber]
        ON [dbo].[Users] ([PhoneNumber])
        WHERE [PhoneNumber] IS NOT NULL;

    PRINT 'Users table created successfully';
END
ELSE
BEGIN
    PRINT 'Users table already exists';
END
GO

-- OtpRequests Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[OtpRequests]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[OtpRequests] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [PhoneNumber] NVARCHAR(20) NOT NULL,
        [Code] NVARCHAR(6) NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [ExpiresAt] DATETIME2 NOT NULL,
        [IsUsed] BIT NOT NULL DEFAULT 0,
        [AttemptCount] INT NOT NULL DEFAULT 0,
        CONSTRAINT [PK_OtpRequests] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    -- Index for looking up OTP by phone and code
    CREATE NONCLUSTERED INDEX [IX_OtpRequests_PhoneNumber_Code]
        ON [dbo].[OtpRequests] ([PhoneNumber], [Code]);

    -- Index for cleanup of expired OTPs
    CREATE NONCLUSTERED INDEX [IX_OtpRequests_ExpiresAt]
        ON [dbo].[OtpRequests] ([ExpiresAt]);

    PRINT 'OtpRequests table created successfully';
END
ELSE
BEGIN
    PRINT 'OtpRequests table already exists';
END
GO

-- OtpRateLimits Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[OtpRateLimits]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[OtpRateLimits] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [PhoneNumber] NVARCHAR(20) NOT NULL,
        [RequestCount] INT NOT NULL DEFAULT 0,
        [WindowStart] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [BlockedUntil] DATETIME2 NULL,
        CONSTRAINT [PK_OtpRateLimits] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    -- Unique index on PhoneNumber
    CREATE UNIQUE NONCLUSTERED INDEX [IX_OtpRateLimits_PhoneNumber]
        ON [dbo].[OtpRateLimits] ([PhoneNumber]);

    PRINT 'OtpRateLimits table created successfully';
END
ELSE
BEGIN
    PRINT 'OtpRateLimits table already exists';
END
GO

PRINT 'All tables created successfully';
GO
