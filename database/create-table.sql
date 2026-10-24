-- Creates the ProductsDb database and the Products table used by the API.
-- Run this once (for example with SQL Server Management Studio,
-- Azure Data Studio, or sqlcmd) before testing the API.
-- It is safe to run again: it skips anything that already exists.

-- 1. Create the database if it doesn't exist yet.
IF DB_ID(N'ProductsDb') IS NULL
BEGIN
    CREATE DATABASE ProductsDb;
END
GO

-- 2. Switch to the new database. Without this, the table would be created in
--    whatever database is currently selected (often "master").
USE ProductsDb;
GO

-- 3. Create the table if it doesn't exist yet.
IF OBJECT_ID(N'dbo.Products', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Products
    (
        Id        NVARCHAR(36)   NOT NULL PRIMARY KEY,  -- GUID as text, set by the API
        Name      NVARCHAR(200)  NOT NULL,
        Price     DECIMAL(18, 2) NOT NULL,
        CreatedAt DATETIME2      NOT NULL               -- always UTC
    );
END
GO
