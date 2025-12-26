-- Create Sites table
CREATE TABLE Sites (
    [Key] NVARCHAR(50) NOT NULL PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500) NULL,
    Url NVARCHAR(255) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    RequiresSubscription BIT NOT NULL DEFAULT 0,
    MonthlyPriceCents BIGINT NULL,
    YearlyPriceCents BIGINT NULL,
    DisplayOrder INT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL
);

-- Insert default sites
INSERT INTO Sites ([Key], Name, Description, Url, IsActive, RequiresSubscription, DisplayOrder)
VALUES
    ('community', 'Pickleball Community', 'Connect with players in your area', 'https://pickleball.community', 1, 0, 1),
    ('college', 'Pickleball College', 'Learn and improve your skills', 'https://pickleball.college', 1, 0, 2),
    ('date', 'Pickleball Date', 'Find your perfect playing partner', 'https://pickleball.date', 1, 0, 3),
    ('jobs', 'Pickleball Jobs', 'Career opportunities in pickleball', 'https://pickleball.jobs', 1, 0, 4);
