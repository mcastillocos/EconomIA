-- =============================================
-- Phase D-F tables: Checklists, Earnings Calls, Learning Screener
-- =============================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ChecklistTemplates')
BEGIN
    CREATE TABLE ChecklistTemplates (
        Id              UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        Name            NVARCHAR(200)    NOT NULL,
        Description     NVARCHAR(1000)   NULL,
        Category        NVARCHAR(50)     NOT NULL, -- company | fund | portfolio | general
        IsBuiltIn       BIT              NOT NULL DEFAULT 0,
        CreatedAt       DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt       DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME()
    );
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ChecklistTemplateItems')
BEGIN
    CREATE TABLE ChecklistTemplateItems (
        Id              UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        TemplateId      UNIQUEIDENTIFIER NOT NULL,
        Text            NVARCHAR(500)    NOT NULL,
        Section         NVARCHAR(100)    NOT NULL,
        [Order]         INT              NOT NULL DEFAULT 0,
        ItemType        NVARCHAR(20)     NOT NULL DEFAULT 'boolean', -- boolean | rating | text | numeric
        HelpText        NVARCHAR(500)    NULL,
        CONSTRAINT FK_ChecklistTemplateItems_Template
            FOREIGN KEY (TemplateId) REFERENCES ChecklistTemplates(Id) ON DELETE CASCADE
    );
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ChecklistInstances')
BEGIN
    CREATE TABLE ChecklistInstances (
        Id              UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        TemplateId      UNIQUEIDENTIFIER NOT NULL,
        EntityType      NVARCHAR(50)     NOT NULL, -- company | fund
        EntityName      NVARCHAR(200)    NOT NULL,
        EntityId        UNIQUEIDENTIFIER NULL,
        Status          NVARCHAR(20)     NOT NULL DEFAULT 'in_progress', -- in_progress | completed
        Notes           NVARCHAR(MAX)    NULL,
        CreatedAt       DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt       DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_ChecklistInstances_Template
            FOREIGN KEY (TemplateId) REFERENCES ChecklistTemplates(Id)
    );
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ChecklistAnswers')
BEGIN
    CREATE TABLE ChecklistAnswers (
        Id              UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        InstanceId      UNIQUEIDENTIFIER NOT NULL,
        TemplateItemId  UNIQUEIDENTIFIER NOT NULL,
        Value           NVARCHAR(500)    NOT NULL,
        Comment         NVARCHAR(1000)   NULL,
        AnsweredAt      DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_ChecklistAnswers_Instance
            FOREIGN KEY (InstanceId) REFERENCES ChecklistInstances(Id) ON DELETE CASCADE,
        CONSTRAINT FK_ChecklistAnswers_TemplateItem
            FOREIGN KEY (TemplateItemId) REFERENCES ChecklistTemplateItems(Id)
    );
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EarningsCalls')
BEGIN
    CREATE TABLE EarningsCalls (
        Id              UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        CompanyName     NVARCHAR(200)    NOT NULL,
        Ticker          NVARCHAR(20)     NULL,
        CompanyId       UNIQUEIDENTIFIER NULL,
        FiscalYear      INT              NOT NULL,
        FiscalQuarter   INT              NOT NULL,
        CallDate        DATETIME2        NOT NULL,
        AudioFilePath   NVARCHAR(500)    NULL,
        TranscriptText  NVARCHAR(MAX)    NULL,
        Summary         NVARCHAR(MAX)    NULL,
        Guidance        NVARCHAR(MAX)    NULL,
        KeyMetrics      NVARCHAR(MAX)    NULL,
        Sentiment       NVARCHAR(20)     NULL, -- positive | neutral | negative
        Status          NVARCHAR(20)     NOT NULL DEFAULT 'pending', -- pending | transcribing | analyzing | completed | failed
        ErrorMessage    NVARCHAR(1000)   NULL,
        DurationSeconds INT              NULL,
        Language        NVARCHAR(10)     NULL,
        CreatedAt       DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt       DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME()
    );
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'InvestorProfiles')
BEGIN
    CREATE TABLE InvestorProfiles (
        Id                    UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        UserId                NVARCHAR(100)    NOT NULL DEFAULT 'default',
        RiskTolerance         NVARCHAR(20)     NOT NULL DEFAULT 'moderate',
        InvestmentHorizon     NVARCHAR(20)     NOT NULL DEFAULT 'medium',
        MaxExpenseRatio       DECIMAL(5,2)     NOT NULL DEFAULT 1.5,
        MinReturn1Y           DECIMAL(8,2)     NOT NULL DEFAULT 0,
        PreferredSectors      NVARCHAR(MAX)    NOT NULL DEFAULT '[]',
        PreferredGeographies  NVARCHAR(MAX)    NOT NULL DEFAULT '[]',
        ExcludedSectors       NVARCHAR(MAX)    NOT NULL DEFAULT '[]',
        InvestmentStyle       NVARCHAR(20)     NOT NULL DEFAULT 'blend',
        AssetPreference       NVARCHAR(20)     NOT NULL DEFAULT 'both',
        EsgPreference         BIT              NOT NULL DEFAULT 0,
        Notes                 NVARCHAR(MAX)    NULL,
        InteractionsCount     INT              NOT NULL DEFAULT 0,
        CreatedAt             DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt             DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME()
    );
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ScreenerRecommendations')
BEGIN
    CREATE TABLE ScreenerRecommendations (
        Id              UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        ProfileId       UNIQUEIDENTIFIER NOT NULL,
        EntityType      NVARCHAR(50)     NOT NULL, -- fund | company
        EntityName      NVARCHAR(200)    NOT NULL,
        Ticker          NVARCHAR(20)     NULL,
        Isin            NVARCHAR(20)     NULL,
        Reason          NVARCHAR(MAX)    NOT NULL,
        Score           DECIMAL(5,2)     NOT NULL, -- 0-100
        Category        NVARCHAR(20)     NOT NULL, -- core | tactical | speculative
        Metrics         NVARCHAR(MAX)    NULL, -- JSON
        Status          NVARCHAR(20)     NOT NULL DEFAULT 'active', -- active | dismissed | saved | invested
        GeneratedAt     DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        ReviewedAt      DATETIME2        NULL,
        CONSTRAINT FK_ScreenerRecommendations_Profile
            FOREIGN KEY (ProfileId) REFERENCES InvestorProfiles(Id)
    );
END

-- Índices útiles
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ChecklistInstances_TemplateId')
    CREATE INDEX IX_ChecklistInstances_TemplateId ON ChecklistInstances(TemplateId);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ChecklistAnswers_InstanceId')
    CREATE INDEX IX_ChecklistAnswers_InstanceId ON ChecklistAnswers(InstanceId);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_EarningsCalls_CompanyName')
    CREATE INDEX IX_EarningsCalls_CompanyName ON EarningsCalls(CompanyName);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ScreenerRecommendations_ProfileId')
    CREATE INDEX IX_ScreenerRecommendations_ProfileId ON ScreenerRecommendations(ProfileId);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_InvestorProfiles_UserId')
    CREATE UNIQUE INDEX IX_InvestorProfiles_UserId ON InvestorProfiles(UserId);
