-- Seed Test Data (Development Only)
-- This script creates test users for development purposes
-- DO NOT run this in production!

USE FTPBAuth;
GO

-- Test user with email only (password: Test@123456)
-- BCrypt hash for 'Test@123456'
IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Email] = 'test@example.com')
BEGIN
    INSERT INTO [dbo].[Users] ([Email], [PasswordHash], [IsEmailVerified], [CreatedAt])
    VALUES ('test@example.com', '$2a$11$K7QvKbHvJjVZmVBgKqPqQeT9FpMVYqvqJLwJQvKXmYpY1mYpY1234', 1, GETUTCDATE());
    PRINT 'Test email user created: test@example.com';
END

-- Test user with phone only
IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [PhoneNumber] = '+1234567890')
BEGIN
    INSERT INTO [dbo].[Users] ([PhoneNumber], [IsPhoneVerified], [CreatedAt])
    VALUES ('+1234567890', 1, GETUTCDATE());
    PRINT 'Test phone user created: +1234567890';
END

-- Test user with both email and phone (password: Test@123456)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Email] = 'fulluser@example.com')
BEGIN
    INSERT INTO [dbo].[Users] ([Email], [PasswordHash], [PhoneNumber], [IsEmailVerified], [IsPhoneVerified], [CreatedAt])
    VALUES ('fulluser@example.com', '$2a$11$K7QvKbHvJjVZmVBgKqPqQeT9FpMVYqvqJLwJQvKXmYpY1mYpY1234', '+9876543210', 1, 1, GETUTCDATE());
    PRINT 'Test full user created: fulluser@example.com / +9876543210';
END

PRINT 'Test data seeding completed';
GO
