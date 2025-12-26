-- Migration: Add Notification tables
-- Date: 2024-12-26

-- MailProfiles table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'MailProfiles')
BEGIN
    CREATE TABLE MailProfiles (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        SmtpHost NVARCHAR(255) NOT NULL,
        SmtpPort INT NOT NULL DEFAULT 587,
        Username NVARCHAR(255) NULL,
        Password NVARCHAR(500) NULL,
        FromEmail NVARCHAR(255) NOT NULL,
        FromName NVARCHAR(100) NULL,
        SecurityMode NVARCHAR(30) NOT NULL DEFAULT 'StartTlsWhenAvailable',
        IsActive BIT NOT NULL DEFAULT 1,
        SiteKey NVARCHAR(50) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NULL
    );

    CREATE INDEX IX_MailProfiles_Name ON MailProfiles(Name);
    CREATE INDEX IX_MailProfiles_SiteKey ON MailProfiles(SiteKey);
    CREATE INDEX IX_MailProfiles_IsActive ON MailProfiles(IsActive);
END
GO

-- NotificationTemplates table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'NotificationTemplates')
BEGIN
    CREATE TABLE NotificationTemplates (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Code NVARCHAR(100) NOT NULL,
        Name NVARCHAR(200) NOT NULL,
        Type NVARCHAR(20) NOT NULL DEFAULT 'Email',
        Language NVARCHAR(10) NOT NULL DEFAULT 'en',
        Subject NVARCHAR(500) NULL,
        Body NVARCHAR(MAX) NOT NULL,
        BodyText NVARCHAR(MAX) NULL,
        SiteKey NVARCHAR(50) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        Description NVARCHAR(500) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NULL
    );

    CREATE INDEX IX_NotificationTemplates_Code ON NotificationTemplates(Code);
    CREATE UNIQUE INDEX IX_NotificationTemplates_Code_Site_Lang ON NotificationTemplates(Code, SiteKey, Language) WHERE SiteKey IS NOT NULL;
    CREATE INDEX IX_NotificationTemplates_SiteKey ON NotificationTemplates(SiteKey);
    CREATE INDEX IX_NotificationTemplates_Type ON NotificationTemplates(Type);
END
GO

-- NotificationTasks table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'NotificationTasks')
BEGIN
    CREATE TABLE NotificationTasks (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Code NVARCHAR(100) NOT NULL,
        Name NVARCHAR(200) NOT NULL,
        Type NVARCHAR(20) NOT NULL DEFAULT 'Email',
        Status NVARCHAR(20) NOT NULL DEFAULT 'Active',
        Priority NVARCHAR(20) NOT NULL DEFAULT 'Normal',
        MailProfileId INT NULL,
        TemplateId INT NULL,
        SiteKey NVARCHAR(50) NULL,
        DefaultRecipients NVARCHAR(1000) NULL,
        CcRecipients NVARCHAR(1000) NULL,
        BccRecipients NVARCHAR(1000) NULL,
        TestEmail NVARCHAR(255) NULL,
        MaxRetries INT NOT NULL DEFAULT 3,
        Description NVARCHAR(500) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NULL,
        CONSTRAINT FK_NotificationTasks_MailProfile FOREIGN KEY (MailProfileId) REFERENCES MailProfiles(Id) ON DELETE SET NULL,
        CONSTRAINT FK_NotificationTasks_Template FOREIGN KEY (TemplateId) REFERENCES NotificationTemplates(Id) ON DELETE SET NULL
    );

    CREATE UNIQUE INDEX IX_NotificationTasks_Code ON NotificationTasks(Code);
    CREATE INDEX IX_NotificationTasks_SiteKey ON NotificationTasks(SiteKey);
    CREATE INDEX IX_NotificationTasks_Status ON NotificationTasks(Status);
END
GO

-- NotificationOutbox table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'NotificationOutbox')
BEGIN
    CREATE TABLE NotificationOutbox (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        TaskId INT NULL,
        Type NVARCHAR(20) NOT NULL DEFAULT 'Email',
        ToList NVARCHAR(1000) NOT NULL,
        CcList NVARCHAR(1000) NULL,
        BccList NVARCHAR(1000) NULL,
        FromEmail NVARCHAR(255) NULL,
        FromName NVARCHAR(100) NULL,
        Subject NVARCHAR(500) NULL,
        BodyHtml NVARCHAR(MAX) NULL,
        BodyText NVARCHAR(MAX) NULL,
        TemplateData NVARCHAR(MAX) NULL,
        Status NVARCHAR(20) NOT NULL DEFAULT 'Pending',
        Priority NVARCHAR(20) NOT NULL DEFAULT 'Normal',
        Attempts INT NOT NULL DEFAULT 0,
        MaxAttempts INT NOT NULL DEFAULT 3,
        LastError NVARCHAR(2000) NULL,
        ScheduledAt DATETIME2 NULL,
        NextRetryAt DATETIME2 NULL,
        SiteKey NVARCHAR(50) NULL,
        UserId INT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NULL,
        CONSTRAINT FK_NotificationOutbox_Task FOREIGN KEY (TaskId) REFERENCES NotificationTasks(Id) ON DELETE SET NULL
    );

    CREATE INDEX IX_NotificationOutbox_Status ON NotificationOutbox(Status);
    CREATE INDEX IX_NotificationOutbox_Priority ON NotificationOutbox(Priority);
    CREATE INDEX IX_NotificationOutbox_ScheduledAt ON NotificationOutbox(ScheduledAt);
    CREATE INDEX IX_NotificationOutbox_NextRetryAt ON NotificationOutbox(NextRetryAt);
    CREATE INDEX IX_NotificationOutbox_SiteKey ON NotificationOutbox(SiteKey);
    CREATE INDEX IX_NotificationOutbox_CreatedAt ON NotificationOutbox(CreatedAt);
END
GO

-- NotificationHistory table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'NotificationHistory')
BEGIN
    CREATE TABLE NotificationHistory (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        OutboxId INT NULL,
        TaskId INT NULL,
        Type NVARCHAR(20) NOT NULL DEFAULT 'Email',
        ToList NVARCHAR(1000) NOT NULL,
        FromEmail NVARCHAR(255) NULL,
        Subject NVARCHAR(500) NULL,
        Status NVARCHAR(20) NOT NULL DEFAULT 'Sent',
        Attempts INT NOT NULL DEFAULT 1,
        ExternalId NVARCHAR(255) NULL,
        ErrorMessage NVARCHAR(2000) NULL,
        SiteKey NVARCHAR(50) NULL,
        UserId INT NULL,
        SentAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        DeliveredAt DATETIME2 NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );

    CREATE INDEX IX_NotificationHistory_Status ON NotificationHistory(Status);
    CREATE INDEX IX_NotificationHistory_SiteKey ON NotificationHistory(SiteKey);
    CREATE INDEX IX_NotificationHistory_SentAt ON NotificationHistory(SentAt);
    CREATE INDEX IX_NotificationHistory_UserId ON NotificationHistory(UserId);
    CREATE INDEX IX_NotificationHistory_CreatedAt ON NotificationHistory(CreatedAt);
END
GO
