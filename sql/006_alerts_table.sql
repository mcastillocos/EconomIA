-- Alerts table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Alerts')
BEGIN
    CREATE TABLE Alerts (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        Name NVARCHAR(200) NOT NULL,
        EntityType NVARCHAR(50) NOT NULL,
        EntityId UNIQUEIDENTIFIER NULL,
        [Condition] NVARCHAR(200) NOT NULL,
        [Operator] NVARCHAR(10) NOT NULL,
        Threshold DECIMAL(18,4) NOT NULL,
        Field NVARCHAR(50) NOT NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        HasTriggered BIT NOT NULL DEFAULT 0,
        LastTriggeredAt DATETIME2 NULL,
        LastMessage NVARCHAR(500) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
END
GO
