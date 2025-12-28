-- Migration: Rename PhoneNumber to Identifier in OtpRequests and OtpRateLimits tables
-- This supports email-based OTP in addition to phone-based OTP
-- Date: 2024-12-24

-- Drop existing indexes on OtpRequests
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_OtpRequests_PhoneNumber_Code' AND object_id = OBJECT_ID('OtpRequests'))
    DROP INDEX IX_OtpRequests_PhoneNumber_Code ON OtpRequests;

-- Drop existing indexes on OtpRateLimits
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_OtpRateLimits_PhoneNumber' AND object_id = OBJECT_ID('OtpRateLimits'))
    DROP INDEX IX_OtpRateLimits_PhoneNumber ON OtpRateLimits;

-- Rename column in OtpRequests
IF EXISTS (SELECT * FROM sys.columns WHERE name = 'PhoneNumber' AND object_id = OBJECT_ID('OtpRequests'))
BEGIN
    EXEC sp_rename 'OtpRequests.PhoneNumber', 'Identifier', 'COLUMN';
END

-- Alter column size in OtpRequests to support emails (255 chars)
ALTER TABLE OtpRequests ALTER COLUMN Identifier NVARCHAR(255) NOT NULL;

-- Rename column in OtpRateLimits
IF EXISTS (SELECT * FROM sys.columns WHERE name = 'PhoneNumber' AND object_id = OBJECT_ID('OtpRateLimits'))
BEGIN
    EXEC sp_rename 'OtpRateLimits.PhoneNumber', 'Identifier', 'COLUMN';
END

-- Alter column size in OtpRateLimits to support emails (255 chars)
ALTER TABLE OtpRateLimits ALTER COLUMN Identifier NVARCHAR(255) NOT NULL;

-- Create new indexes
CREATE INDEX IX_OtpRequests_Identifier_Code ON OtpRequests (Identifier, Code);
CREATE UNIQUE INDEX IX_OtpRateLimits_Identifier ON OtpRateLimits (Identifier);

PRINT 'Migration completed: PhoneNumber renamed to Identifier in OtpRequests and OtpRateLimits tables';
