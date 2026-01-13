-- Create UserProfiles and UserSites Tables for FuntimeIdentity
-- Run this script after creating the base tables

USE FuntimeIdentity;
GO

-- UserProfiles Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[UserProfiles]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[UserProfiles] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NOT NULL,
        [FirstName] NVARCHAR(100) NULL,
        [LastName] NVARCHAR(100) NULL,
        [DisplayName] NVARCHAR(255) NULL,
        [AvatarUrl] NVARCHAR(500) NULL,
        [City] NVARCHAR(100) NULL,
        [State] NVARCHAR(50) NULL,
        [Country] NVARCHAR(50) NULL,
        [SkillLevel] DECIMAL(3,1) NULL,
        [Bio] NVARCHAR(1000) NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] DATETIME2 NULL,
        CONSTRAINT [PK_UserProfiles] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_UserProfiles_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE CASCADE
    );

    -- Unique index on UserId (one profile per user)
    CREATE UNIQUE NONCLUSTERED INDEX [IX_UserProfiles_UserId]
        ON [dbo].[UserProfiles] ([UserId]);

    PRINT 'UserProfiles table created successfully';
END
ELSE
BEGIN
    PRINT 'UserProfiles table already exists';
END
GO

-- UserSites Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[UserSites]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[UserSites] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NOT NULL,
        [SiteKey] NVARCHAR(50) NOT NULL,
        [JoinedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [IsActive] BIT NOT NULL DEFAULT 1,
        [Role] NVARCHAR(50) NOT NULL DEFAULT 'member',
        CONSTRAINT [PK_UserSites] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_UserSites_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE CASCADE
    );

    -- Unique index on UserId + SiteKey (one membership per site per user)
    CREATE UNIQUE NONCLUSTERED INDEX [IX_UserSites_UserId_SiteKey]
        ON [dbo].[UserSites] ([UserId], [SiteKey]);

    -- Index for looking up by site
    CREATE NONCLUSTERED INDEX [IX_UserSites_SiteKey]
        ON [dbo].[UserSites] ([SiteKey]);

    PRINT 'UserSites table created successfully';
END
ELSE
BEGIN
    PRINT 'UserSites table already exists';
END
GO

PRINT 'Profile and Site tables created successfully';
GO
