-- Cleanup Expired OTPs
-- This script can be run periodically to clean up expired OTP records

USE FTPBAuth;
GO

-- Delete expired OTP requests older than 24 hours
DELETE FROM [dbo].[OtpRequests]
WHERE [ExpiresAt] < DATEADD(HOUR, -24, GETUTCDATE());

-- Reset rate limits where the window has expired (older than 1 hour)
UPDATE [dbo].[OtpRateLimits]
SET [RequestCount] = 0,
    [WindowStart] = GETUTCDATE(),
    [BlockedUntil] = NULL
WHERE [WindowStart] < DATEADD(HOUR, -1, GETUTCDATE());

PRINT 'Cleanup completed';
GO
