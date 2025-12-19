-- Create ExternalLogins Table for OAuth provider support
-- Run this script after 002_CreateTables.sql

USE FTPBAuth;
GO

-- ExternalLogins Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ExternalLogins]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[ExternalLogins] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NOT NULL,
        [Provider] NVARCHAR(50) NOT NULL,
        [ProviderUserId] NVARCHAR(255) NOT NULL,
        [ProviderEmail] NVARCHAR(255) NULL,
        [ProviderDisplayName] NVARCHAR(255) NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [LastUsedAt] DATETIME2 NULL,
        CONSTRAINT [PK_ExternalLogins] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_ExternalLogins_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE CASCADE
    );

    -- Each provider+providerUserId combination must be unique (one provider account = one user)
    CREATE UNIQUE NONCLUSTERED INDEX [IX_ExternalLogins_Provider_ProviderUserId]
        ON [dbo].[ExternalLogins] ([Provider], [ProviderUserId]);

    -- Each user can only have one login per provider
    CREATE UNIQUE NONCLUSTERED INDEX [IX_ExternalLogins_UserId_Provider]
        ON [dbo].[ExternalLogins] ([UserId], [Provider]);

    -- Index for looking up by user
    CREATE NONCLUSTERED INDEX [IX_ExternalLogins_UserId]
        ON [dbo].[ExternalLogins] ([UserId]);

    PRINT 'ExternalLogins table created successfully';
END
ELSE
BEGIN
    PRINT 'ExternalLogins table already exists';
END
GO

PRINT 'External logins table setup completed';
GO
