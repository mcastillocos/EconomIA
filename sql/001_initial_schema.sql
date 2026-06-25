-- ============================================================
-- EconomIA - Initial Database Setup
-- Creates the database and base schema
-- ============================================================

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'EconomIA')
BEGIN
    CREATE DATABASE EconomIA;
END
GO

USE EconomIA;
GO

-- Funds table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Funds')
BEGIN
    CREATE TABLE Funds (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        Isin NVARCHAR(12) NOT NULL,
        Name NVARCHAR(500) NOT NULL,
        Category NVARCHAR(200) NULL,
        ManagementCompany NVARCHAR(300) NULL,
        RiskLevel INT NOT NULL,
        NetAssetValue DECIMAL(18, 6) NOT NULL,
        Currency NVARCHAR(3) NOT NULL,
        ExpenseRatio DECIMAL(8, 4) NOT NULL,
        Rating INT NOT NULL DEFAULT 0,
        RankingPosition INT NOT NULL DEFAULT 0,
        LastUpdated DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );

    CREATE UNIQUE INDEX IX_Funds_Isin ON Funds(Isin);
    CREATE INDEX IX_Funds_RankingPosition ON Funds(RankingPosition);
    CREATE INDEX IX_Funds_RiskLevel ON Funds(RiskLevel);
END
GO

-- Fund Performances table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'FundPerformances')
BEGIN
    CREATE TABLE FundPerformances (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        FundId UNIQUEIDENTIFIER NOT NULL,
        Return1Month DECIMAL(8, 4) NULL,
        Return3Months DECIMAL(8, 4) NULL,
        Return6Months DECIMAL(8, 4) NULL,
        Return1Year DECIMAL(8, 4) NULL,
        Return3Years DECIMAL(8, 4) NULL,
        Return5Years DECIMAL(8, 4) NULL,
        Volatility DECIMAL(8, 4) NULL,
        SharpeRatio DECIMAL(8, 4) NULL,
        RecordedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT FK_FundPerformances_Funds FOREIGN KEY (FundId) 
            REFERENCES Funds(Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_FundPerformances_FundId ON FundPerformances(FundId);
    CREATE INDEX IX_FundPerformances_RecordedAt ON FundPerformances(RecordedAt);
END
GO

-- Market Sectors table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'MarketSectors')
BEGIN
    CREATE TABLE MarketSectors (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        Name NVARCHAR(200) NOT NULL,
        Description NVARCHAR(500) NULL
    );
END
GO
