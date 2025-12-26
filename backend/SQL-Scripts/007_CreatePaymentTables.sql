-- Create Payment Tables for FuntimeIdentity (Stripe Integration)
-- Run this script after creating the base tables

USE FuntimeIdentity;
GO

-- PaymentCustomers Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PaymentCustomers]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[PaymentCustomers] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NOT NULL,
        [StripeCustomerId] NVARCHAR(255) NOT NULL,
        [Email] NVARCHAR(255) NULL,
        [Name] NVARCHAR(255) NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] DATETIME2 NULL,
        CONSTRAINT [PK_PaymentCustomers] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_PaymentCustomers_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE CASCADE
    );

    -- Unique index on UserId (one payment customer per user)
    CREATE UNIQUE NONCLUSTERED INDEX [IX_PaymentCustomers_UserId]
        ON [dbo].[PaymentCustomers] ([UserId]);

    -- Unique index on StripeCustomerId
    CREATE UNIQUE NONCLUSTERED INDEX [IX_PaymentCustomers_StripeCustomerId]
        ON [dbo].[PaymentCustomers] ([StripeCustomerId]);

    PRINT 'PaymentCustomers table created successfully';
END
ELSE
BEGIN
    PRINT 'PaymentCustomers table already exists';
END
GO

-- PaymentMethods Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PaymentMethods]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[PaymentMethods] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [PaymentCustomerId] INT NOT NULL,
        [StripePaymentMethodId] NVARCHAR(255) NOT NULL,
        [Type] NVARCHAR(50) NOT NULL DEFAULT 'card',
        [CardBrand] NVARCHAR(50) NULL,
        [CardLast4] NVARCHAR(4) NULL,
        [CardExpMonth] INT NULL,
        [CardExpYear] INT NULL,
        [IsDefault] BIT NOT NULL DEFAULT 0,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_PaymentMethods] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_PaymentMethods_PaymentCustomers] FOREIGN KEY ([PaymentCustomerId]) REFERENCES [dbo].[PaymentCustomers] ([Id]) ON DELETE CASCADE
    );

    -- Unique index on StripePaymentMethodId
    CREATE UNIQUE NONCLUSTERED INDEX [IX_PaymentMethods_StripePaymentMethodId]
        ON [dbo].[PaymentMethods] ([StripePaymentMethodId]);

    -- Index for looking up by customer
    CREATE NONCLUSTERED INDEX [IX_PaymentMethods_PaymentCustomerId]
        ON [dbo].[PaymentMethods] ([PaymentCustomerId]);

    PRINT 'PaymentMethods table created successfully';
END
ELSE
BEGIN
    PRINT 'PaymentMethods table already exists';
END
GO

-- Subscriptions Table (must be created before Payments due to FK)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Subscriptions]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Subscriptions] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [PaymentCustomerId] INT NOT NULL,
        [StripeSubscriptionId] NVARCHAR(255) NOT NULL,
        [StripePriceId] NVARCHAR(255) NULL,
        [StripeProductId] NVARCHAR(255) NULL,
        [Status] NVARCHAR(50) NOT NULL DEFAULT 'active',
        [PlanName] NVARCHAR(100) NULL,
        [SiteKey] NVARCHAR(50) NULL,
        [AmountCents] BIGINT NULL,
        [Currency] NVARCHAR(3) NOT NULL DEFAULT 'usd',
        [Interval] NVARCHAR(20) NULL,
        [CurrentPeriodStart] DATETIME2 NULL,
        [CurrentPeriodEnd] DATETIME2 NULL,
        [CanceledAt] DATETIME2 NULL,
        [CancelAt] DATETIME2 NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] DATETIME2 NULL,
        CONSTRAINT [PK_Subscriptions] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_Subscriptions_PaymentCustomers] FOREIGN KEY ([PaymentCustomerId]) REFERENCES [dbo].[PaymentCustomers] ([Id]) ON DELETE CASCADE
    );

    -- Unique index on StripeSubscriptionId
    CREATE UNIQUE NONCLUSTERED INDEX [IX_Subscriptions_StripeSubscriptionId]
        ON [dbo].[Subscriptions] ([StripeSubscriptionId]);

    -- Index for looking up by customer
    CREATE NONCLUSTERED INDEX [IX_Subscriptions_PaymentCustomerId]
        ON [dbo].[Subscriptions] ([PaymentCustomerId]);

    -- Index for looking up by status
    CREATE NONCLUSTERED INDEX [IX_Subscriptions_Status]
        ON [dbo].[Subscriptions] ([Status]);

    PRINT 'Subscriptions table created successfully';
END
ELSE
BEGIN
    PRINT 'Subscriptions table already exists';
END
GO

-- Payments Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Payments]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Payments] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [PaymentCustomerId] INT NOT NULL,
        [StripePaymentId] NVARCHAR(255) NOT NULL,
        [AmountCents] BIGINT NOT NULL,
        [Currency] NVARCHAR(3) NOT NULL DEFAULT 'usd',
        [Status] NVARCHAR(50) NOT NULL DEFAULT 'pending',
        [Description] NVARCHAR(500) NULL,
        [SiteKey] NVARCHAR(50) NULL,
        [SubscriptionId] INT NULL,
        [Metadata] NVARCHAR(MAX) NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] DATETIME2 NULL,
        CONSTRAINT [PK_Payments] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_Payments_PaymentCustomers] FOREIGN KEY ([PaymentCustomerId]) REFERENCES [dbo].[PaymentCustomers] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Payments_Subscriptions] FOREIGN KEY ([SubscriptionId]) REFERENCES [dbo].[Subscriptions] ([Id])
    );

    -- Unique index on StripePaymentId
    CREATE UNIQUE NONCLUSTERED INDEX [IX_Payments_StripePaymentId]
        ON [dbo].[Payments] ([StripePaymentId]);

    -- Index for looking up by customer
    CREATE NONCLUSTERED INDEX [IX_Payments_PaymentCustomerId]
        ON [dbo].[Payments] ([PaymentCustomerId]);

    -- Index for looking up by status
    CREATE NONCLUSTERED INDEX [IX_Payments_Status]
        ON [dbo].[Payments] ([Status]);

    -- Index for looking up by date
    CREATE NONCLUSTERED INDEX [IX_Payments_CreatedAt]
        ON [dbo].[Payments] ([CreatedAt] DESC);

    PRINT 'Payments table created successfully';
END
ELSE
BEGIN
    PRINT 'Payments table already exists';
END
GO

PRINT 'All payment tables created successfully';
GO
