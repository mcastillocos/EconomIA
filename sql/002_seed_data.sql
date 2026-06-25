-- ============================================================
-- EconomIA - Seed Data
-- Sample funds for development/testing
-- ============================================================

USE EconomIA;
GO

-- Insert sample market sectors
INSERT INTO MarketSectors (Id, Name, Description) VALUES
    (NEWID(), 'Renta Variable Global', 'Fondos de renta variable diversificados globalmente'),
    (NEWID(), 'Renta Fija', 'Fondos de bonos y deuda'),
    (NEWID(), 'Mixto', 'Fondos que combinan renta variable y fija'),
    (NEWID(), 'Tecnologia', 'Fondos especializados en sector tecnologico'),
    (NEWID(), 'Emergentes', 'Fondos de mercados emergentes'),
    (NEWID(), 'ESG', 'Fondos con criterios ambientales, sociales y de gobernanza'),
    (NEWID(), 'Indexado S&P500', 'Fondos indexados al S&P 500'),
    (NEWID(), 'Europa', 'Fondos de renta variable europea'),
    (NEWID(), 'Asia-Pacifico', 'Fondos de mercados asiaticos'),
    (NEWID(), 'Materias Primas', 'Fondos de commodities');
GO

-- Insert sample funds
INSERT INTO Funds (Id, Isin, Name, Category, ManagementCompany, RiskLevel, NetAssetValue, Currency, ExpenseRatio, Rating, RankingPosition, LastUpdated) VALUES
    (NEWID(), 'IE00B4L5Y983', 'iShares Core MSCI World', 'Renta Variable Global', 'BlackRock', 5, 89.34, 'EUR', 0.20, 5, 1, GETUTCDATE()),
    (NEWID(), 'IE00B5BMR087', 'iShares Core S&P 500', 'Indexado S&P500', 'BlackRock', 5, 512.78, 'USD', 0.07, 5, 2, GETUTCDATE()),
    (NEWID(), 'LU0996182563', 'Amundi Index MSCI World', 'Renta Variable Global', 'Amundi', 5, 245.12, 'EUR', 0.18, 4, 3, GETUTCDATE()),
    (NEWID(), 'IE00BYX2JD69', 'Vanguard FTSE All-World', 'Renta Variable Global', 'Vanguard', 5, 108.56, 'EUR', 0.22, 5, 4, GETUTCDATE()),
    (NEWID(), 'LU1681043599', 'Amundi MSCI Emerging Markets', 'Emergentes', 'Amundi', 6, 45.23, 'EUR', 0.20, 3, 5, GETUTCDATE()),
    (NEWID(), 'IE00B4L5YC18', 'iShares MSCI EM', 'Emergentes', 'BlackRock', 6, 38.90, 'USD', 0.18, 3, 6, GETUTCDATE()),
    (NEWID(), 'LU0290358497', 'Xtrackers Euro Stoxx 50', 'Europa', 'DWS', 5, 78.45, 'EUR', 0.09, 4, 7, GETUTCDATE()),
    (NEWID(), 'IE00B1XNHC34', 'iShares Global Clean Energy', 'ESG', 'BlackRock', 6, 12.34, 'USD', 0.65, 3, 8, GETUTCDATE()),
    (NEWID(), 'LU0959211243', 'Fidelity Global Technology', 'Tecnologia', 'Fidelity', 6, 34.67, 'EUR', 1.50, 4, 9, GETUTCDATE()),
    (NEWID(), 'ES0152743003', 'Cobas Seleccion FI', 'Renta Variable Global', 'Cobas AM', 5, 98.76, 'EUR', 1.75, 3, 10, GETUTCDATE());
GO
