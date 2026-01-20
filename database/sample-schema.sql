-- Sample Database Schema for WSC.DataAccess
-- This script creates sample tables for demonstration purposes

USE master;
GO

-- Create database if it doesn't exist
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'SampleDb')
BEGIN
    CREATE DATABASE SampleDb;
END
GO

USE SampleDb;
GO

-- Create Users table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Users] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [Username] NVARCHAR(50) NOT NULL UNIQUE,
        [Email] NVARCHAR(100) NOT NULL,
        [FullName] NVARCHAR(100) NOT NULL,
        [CreatedDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [LastLoginDate] DATETIME2 NULL,
        [IsActive] BIT NOT NULL DEFAULT 1
    );

    -- Create index on Username
    CREATE INDEX IX_Users_Username ON [dbo].[Users]([Username]);

    -- Create index on Email
    CREATE INDEX IX_Users_Email ON [dbo].[Users]([Email]);
END
GO

-- Create Products table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Products]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Products] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [ProductCode] NVARCHAR(50) NOT NULL UNIQUE,
        [ProductName] NVARCHAR(200) NOT NULL,
        [Description] NVARCHAR(1000) NULL,
        [Price] DECIMAL(18,2) NOT NULL,
        [StockQuantity] INT NOT NULL DEFAULT 0,
        [Category] NVARCHAR(100) NULL,
        [CreatedDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [UpdatedDate] DATETIME2 NULL,
        [IsActive] BIT NOT NULL DEFAULT 1
    );

    -- Create index on ProductCode
    CREATE INDEX IX_Products_ProductCode ON [dbo].[Products]([ProductCode]);

    -- Create index on Category
    CREATE INDEX IX_Products_Category ON [dbo].[Products]([Category]);
END
GO

-- Insert sample data into Users
IF NOT EXISTS (SELECT * FROM [dbo].[Users])
BEGIN
    INSERT INTO [dbo].[Users] ([Username], [Email], [FullName], [CreatedDate], [IsActive])
    VALUES
        ('admin', 'admin@example.com', 'System Administrator', GETDATE(), 1),
        ('john.doe', 'john.doe@example.com', 'John Doe', GETDATE(), 1),
        ('jane.smith', 'jane.smith@example.com', 'Jane Smith', GETDATE(), 1),
        ('bob.wilson', 'bob.wilson@example.com', 'Bob Wilson', GETDATE(), 1),
        ('alice.jones', 'alice.jones@example.com', 'Alice Jones', GETDATE(), 1);
END
GO

-- Insert sample data into Products
IF NOT EXISTS (SELECT * FROM [dbo].[Products])
BEGIN
    INSERT INTO [dbo].[Products] ([ProductCode], [ProductName], [Description], [Price], [StockQuantity], [Category], [CreatedDate], [IsActive])
    VALUES
        ('LAPTOP001', 'Dell XPS 15', 'High-performance laptop for professionals', 1299.99, 50, 'Electronics', GETDATE(), 1),
        ('LAPTOP002', 'MacBook Pro 16', 'Apple MacBook Pro with M1 chip', 2499.99, 30, 'Electronics', GETDATE(), 1),
        ('PHONE001', 'iPhone 13 Pro', 'Latest iPhone model', 999.99, 100, 'Electronics', GETDATE(), 1),
        ('PHONE002', 'Samsung Galaxy S21', 'Android flagship phone', 799.99, 75, 'Electronics', GETDATE(), 1),
        ('TABLET001', 'iPad Air', 'Lightweight tablet', 599.99, 60, 'Electronics', GETDATE(), 1),
        ('MOUSE001', 'Logitech MX Master 3', 'Ergonomic wireless mouse', 99.99, 200, 'Accessories', GETDATE(), 1),
        ('KEYBOARD001', 'Mechanical Keyboard RGB', 'Gaming keyboard with RGB', 149.99, 150, 'Accessories', GETDATE(), 1),
        ('MONITOR001', 'Dell UltraSharp 27"', '4K monitor for professionals', 549.99, 40, 'Electronics', GETDATE(), 1),
        ('HEADPHONE001', 'Sony WH-1000XM4', 'Noise-cancelling headphones', 349.99, 80, 'Accessories', GETDATE(), 1),
        ('WEBCAM001', 'Logitech C920', 'HD webcam for video calls', 79.99, 120, 'Accessories', GETDATE(), 1);
END
GO

-- Create a sample stored procedure
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_GetTopSellingProducts]') AND type in (N'P', N'PC'))
BEGIN
    DROP PROCEDURE [dbo].[usp_GetTopSellingProducts];
END
GO

CREATE PROCEDURE [dbo].[usp_GetTopSellingProducts]
    @Top INT = 10
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@Top)
        [Id],
        [ProductCode],
        [ProductName],
        [Description],
        [Price],
        [StockQuantity],
        [Category],
        [CreatedDate],
        [UpdatedDate],
        [IsActive]
    FROM [dbo].[Products]
    WHERE [IsActive] = 1
    ORDER BY [StockQuantity] DESC;
END
GO

PRINT 'Database schema created successfully!';
PRINT 'Sample data inserted!';
GO
