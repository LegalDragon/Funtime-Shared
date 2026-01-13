-- Add SystemRole column to Users table
-- SystemRole: "SU" for super admin, NULL for regular users

ALTER TABLE Users ADD SystemRole NVARCHAR(10) NULL;

-- Create index for quick lookups of admin users
CREATE INDEX IX_Users_SystemRole ON Users (SystemRole) WHERE SystemRole IS NOT NULL;
