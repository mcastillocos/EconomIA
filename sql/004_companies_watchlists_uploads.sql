-- ============================================================
-- EconomIA - MVP1: Companies, Watchlists, Documents & Metrics
-- Additive migration - does NOT alter existing tables
-- ============================================================

USE EconomIA;
GO

-- Companies table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Companies')
BEGIN
    CREATE TABLE Companies (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        Name NVARCHAR(500) NOT NULL,
        Ticker NVARCHAR(20) NULL,
        Isin NVARCHAR(12) NULL,
        Market NVARCHAR(100) NULL,
        Country NVARCHAR(100) NULL,
        Sector NVARCHAR(200) NULL,
        Industry NVARCHAR(200) NULL,
        Currency NVARCHAR(3) NULL,
        Competitors NVARCHAR(MAX) NULL,
        RelevantUrls NVARCHAR(MAX) NULL,
        PreferredSource NVARCHAR(200) NULL,
        Notes NVARCHAR(MAX) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );

    CREATE INDEX IX_Companies_Ticker ON Companies(Ticker);
    CREATE INDEX IX_Companies_Isin ON Companies(Isin);
    CREATE INDEX IX_Companies_Sector ON Companies(Sector);
    CREATE INDEX IX_Companies_Country ON Companies(Country);
END
GO

-- Watchlists table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Watchlists')
BEGIN
    CREATE TABLE Watchlists (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        Name NVARCHAR(200) NOT NULL,
        Description NVARCHAR(1000) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
END
GO

-- WatchlistItems table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'WatchlistItems')
BEGIN
    CREATE TABLE WatchlistItems (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        WatchlistId UNIQUEIDENTIFIER NOT NULL,
        EntityType NVARCHAR(50) NOT NULL, -- 'fund' | 'company'
        EntityId UNIQUEIDENTIFIER NOT NULL,
        Priority INT NOT NULL DEFAULT 0,
        PositionType NVARCHAR(50) NOT NULL DEFAULT 'watch', -- 'real' | 'watch'
        Thesis NVARCHAR(MAX) NULL,
        Notes NVARCHAR(MAX) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT FK_WatchlistItems_Watchlists FOREIGN KEY (WatchlistId)
            REFERENCES Watchlists(Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_WatchlistItems_WatchlistId ON WatchlistItems(WatchlistId);
    CREATE INDEX IX_WatchlistItems_EntityType_EntityId ON WatchlistItems(EntityType, EntityId);
END
GO

-- UploadedDocuments table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UploadedDocuments')
BEGIN
    CREATE TABLE UploadedDocuments (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        EntityType NVARCHAR(50) NOT NULL, -- 'fund' | 'company' | 'sector' | 'portfolio'
        EntityId UNIQUEIDENTIFIER NULL,
        FileName NVARCHAR(500) NOT NULL,
        FileType NVARCHAR(50) NOT NULL, -- 'csv' | 'excel' | 'pdf' | 'transcript' | 'audio'
        Source NVARCHAR(200) NULL,
        FilePath NVARCHAR(1000) NOT NULL,
        FileSize BIGINT NOT NULL DEFAULT 0,
        UploadDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        Status NVARCHAR(50) NOT NULL DEFAULT 'pending', -- 'pending' | 'processing' | 'completed' | 'failed'
        ExtractedText NVARCHAR(MAX) NULL,
        Summary NVARCHAR(MAX) NULL,
        Metadata NVARCHAR(MAX) NULL,
        ErrorMessage NVARCHAR(MAX) NULL
    );

    CREATE INDEX IX_UploadedDocuments_EntityType_EntityId ON UploadedDocuments(EntityType, EntityId);
    CREATE INDEX IX_UploadedDocuments_Status ON UploadedDocuments(Status);
    CREATE INDEX IX_UploadedDocuments_UploadDate ON UploadedDocuments(UploadDate DESC);
END
GO

-- FinancialMetrics table (normalized data)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'FinancialMetrics')
BEGIN
    CREATE TABLE FinancialMetrics (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        EntityType NVARCHAR(50) NOT NULL, -- 'fund' | 'company' | 'sector'
        EntityId UNIQUEIDENTIFIER NULL,
        Ticker NVARCHAR(20) NULL,
        Isin NVARCHAR(12) NULL,
        MetricName NVARCHAR(200) NOT NULL,
        Value DECIMAL(18, 6) NOT NULL,
        Period NVARCHAR(50) NULL,
        Year INT NULL,
        Quarter INT NULL,
        Currency NVARCHAR(3) NULL,
        Source NVARCHAR(200) NULL,
        SourceType NVARCHAR(50) NULL, -- 'csv' | 'excel' | 'pdf' | 'manual' | etc.
        FileName NVARCHAR(500) NULL,
        Page NVARCHAR(50) NULL,
        Row NVARCHAR(50) NULL,
        Url NVARCHAR(2000) NULL,
        Confidence NVARCHAR(20) NOT NULL DEFAULT 'medium', -- 'high' | 'medium' | 'low'
        RawText NVARCHAR(MAX) NULL,
        Validated BIT NOT NULL DEFAULT 0,
        ValidatedAt DATETIME2 NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );

    CREATE INDEX IX_FinancialMetrics_EntityType_EntityId ON FinancialMetrics(EntityType, EntityId);
    CREATE INDEX IX_FinancialMetrics_MetricName ON FinancialMetrics(MetricName);
    CREATE INDEX IX_FinancialMetrics_Ticker ON FinancialMetrics(Ticker);
    CREATE INDEX IX_FinancialMetrics_Year_Quarter ON FinancialMetrics(Year, Quarter);
    CREATE INDEX IX_FinancialMetrics_Source ON FinancialMetrics(Source);
    CREATE INDEX IX_FinancialMetrics_Validated ON FinancialMetrics(Validated);
END
GO

-- AIReports table (stub for MVP2)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AIReports')
BEGIN
    CREATE TABLE AIReports (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        EntityType NVARCHAR(50) NOT NULL,
        EntityId UNIQUEIDENTIFIER NULL,
        ReportType NVARCHAR(100) NOT NULL,
        Title NVARCHAR(500) NOT NULL,
        Content NVARCHAR(MAX) NULL,
        Sources NVARCHAR(MAX) NULL,
        Confidence NVARCHAR(20) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );

    CREATE INDEX IX_AIReports_EntityType_EntityId ON AIReports(EntityType, EntityId);
    CREATE INDEX IX_AIReports_ReportType ON AIReports(ReportType);
END
GO

-- AgentRuns table (stub for MVP2)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AgentRuns')
BEGIN
    CREATE TABLE AgentRuns (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        AgentName NVARCHAR(200) NOT NULL,
        Status NVARCHAR(50) NOT NULL DEFAULT 'pending', -- 'pending' | 'running' | 'completed' | 'failed'
        Input NVARCHAR(MAX) NULL,
        Output NVARCHAR(MAX) NULL,
        Sources NVARCHAR(MAX) NULL,
        Error NVARCHAR(MAX) NULL,
        StartedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CompletedAt DATETIME2 NULL
    );

    CREATE INDEX IX_AgentRuns_AgentName ON AgentRuns(AgentName);
    CREATE INDEX IX_AgentRuns_Status ON AgentRuns(Status);
    CREATE INDEX IX_AgentRuns_StartedAt ON AgentRuns(StartedAt DESC);
END
GO
