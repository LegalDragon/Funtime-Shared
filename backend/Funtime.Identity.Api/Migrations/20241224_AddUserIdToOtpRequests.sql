-- Migration: Add UserId column to OtpRequests table
-- Stores matched user ID when OTP is sent (null if no existing user)
-- Date: 2024-12-24

-- Add UserId column to OtpRequests
IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'UserId' AND object_id = OBJECT_ID('OtpRequests'))
BEGIN
    ALTER TABLE OtpRequests ADD UserId INT NULL;
END

-- Add foreign key constraint
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_OtpRequests_Users_UserId')
BEGIN
    ALTER TABLE OtpRequests
    ADD CONSTRAINT FK_OtpRequests_Users_UserId
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE SET NULL;
END

-- Add index for UserId lookups
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_OtpRequests_UserId' AND object_id = OBJECT_ID('OtpRequests'))
BEGIN
    CREATE INDEX IX_OtpRequests_UserId ON OtpRequests (UserId);
END

PRINT 'Migration completed: UserId column added to OtpRequests table';
