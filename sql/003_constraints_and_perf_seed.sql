-- ============================================================
-- EconomIA - 003 - Add constraints + seed FundPerformances
-- Adds CHECK constraints matching Value Object rules
-- Seeds performance data for existing funds
-- ============================================================

USE EconomIA;
GO

-- ── CHECK constraints (alinear SQL con Value Objects del dominio) ────────

-- RiskLevel enum: VeryLow(1)..VeryHigh(7)
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Funds_RiskLevel')
    ALTER TABLE Funds ADD CONSTRAINT CK_Funds_RiskLevel CHECK (RiskLevel BETWEEN 1 AND 7);
GO

-- FundRating enum: Unrated(0)..FiveStars(5)
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Funds_Rating')
    ALTER TABLE Funds ADD CONSTRAINT CK_Funds_Rating CHECK (Rating BETWEEN 0 AND 5);
GO

-- Currency ISO 4217: exactly 3 uppercase letters
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Funds_Currency')
    ALTER TABLE Funds ADD CONSTRAINT CK_Funds_Currency CHECK (LEN(Currency) = 3);
GO

-- ISIN: exactly 12 characters
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Funds_Isin')
    ALTER TABLE Funds ADD CONSTRAINT CK_Funds_Isin CHECK (LEN(Isin) = 12);
GO

-- Expense ratio >= 0
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Funds_ExpenseRatio')
    ALTER TABLE Funds ADD CONSTRAINT CK_Funds_ExpenseRatio CHECK (ExpenseRatio >= 0);
GO

-- SharpeRatio not null constraint (decimal in entity, no Percentage VO)
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_FundPerformances_SharpeRatio')
    ALTER TABLE FundPerformances ADD CONSTRAINT CK_FundPerformances_SharpeRatio CHECK (SharpeRatio IS NOT NULL);
GO

-- ── Seed FundPerformances for existing funds ────────────────────────────

-- Only insert if no performances exist yet
IF NOT EXISTS (SELECT 1 FROM FundPerformances)
BEGIN
    INSERT INTO FundPerformances (Id, FundId, Return1Month, Return3Months, Return6Months, Return1Year, Return3Years, Return5Years, Volatility, SharpeRatio, RecordedAt)
    SELECT 
        NEWID(),
        f.Id,
        perf.Return1Month,
        perf.Return3Months,
        perf.Return6Months,
        perf.Return1Year,
        perf.Return3Years,
        perf.Return5Years,
        perf.Volatility,
        perf.SharpeRatio,
        GETUTCDATE()
    FROM Funds f
    CROSS APPLY (
        VALUES 
            -- Performance data per ISIN
            (CASE f.Isin
                WHEN 'IE00B4L5Y983' THEN 2.10  -- iShares Core MSCI World
                WHEN 'IE00B5BMR087' THEN 2.80  -- iShares Core S&P 500
                WHEN 'LU0996182563' THEN 1.90  -- Amundi Index MSCI World
                WHEN 'IE00BYX2JD69' THEN 2.00  -- Vanguard FTSE All-World
                WHEN 'LU1681043599' THEN 0.50  -- Amundi MSCI EM
                WHEN 'IE00B4L5YC18' THEN 0.40  -- iShares MSCI EM
                WHEN 'LU0290358497' THEN 1.50  -- Xtrackers Euro Stoxx 50
                WHEN 'IE00B1XNHC34' THEN -1.20 -- iShares Global Clean Energy
                WHEN 'LU0959211243' THEN 3.10  -- Fidelity Global Technology
                WHEN 'ES0152743003' THEN 1.80  -- Cobas Seleccion
                ELSE 1.00
            END,
            CASE f.Isin
                WHEN 'IE00B4L5Y983' THEN 5.40
                WHEN 'IE00B5BMR087' THEN 7.20
                WHEN 'LU0996182563' THEN 5.10
                WHEN 'IE00BYX2JD69' THEN 5.30
                WHEN 'LU1681043599' THEN 1.80
                WHEN 'IE00B4L5YC18' THEN 1.50
                WHEN 'LU0290358497' THEN 4.20
                WHEN 'IE00B1XNHC34' THEN -3.40
                WHEN 'LU0959211243' THEN 8.50
                WHEN 'ES0152743003' THEN 4.80
                ELSE 3.00
            END,
            CASE f.Isin
                WHEN 'IE00B4L5Y983' THEN 8.70
                WHEN 'IE00B5BMR087' THEN 12.30
                WHEN 'LU0996182563' THEN 8.20
                WHEN 'IE00BYX2JD69' THEN 8.50
                WHEN 'LU1681043599' THEN 3.10
                WHEN 'IE00B4L5YC18' THEN 2.80
                WHEN 'LU0290358497' THEN 6.80
                WHEN 'IE00B1XNHC34' THEN -5.20
                WHEN 'LU0959211243' THEN 14.60
                WHEN 'ES0152743003' THEN 7.90
                ELSE 5.00
            END,
            CASE f.Isin
                WHEN 'IE00B4L5Y983' THEN 18.50
                WHEN 'IE00B5BMR087' THEN 24.80
                WHEN 'LU0996182563' THEN 17.90
                WHEN 'IE00BYX2JD69' THEN 18.20
                WHEN 'LU1681043599' THEN 6.40
                WHEN 'IE00B4L5YC18' THEN 5.90
                WHEN 'LU0290358497' THEN 12.30
                WHEN 'IE00B1XNHC34' THEN -8.50
                WHEN 'LU0959211243' THEN 28.70
                WHEN 'ES0152743003' THEN 15.40
                ELSE 10.00
            END,
            CASE f.Isin
                WHEN 'IE00B4L5Y983' THEN 42.30
                WHEN 'IE00B5BMR087' THEN 56.10
                WHEN 'LU0996182563' THEN 40.80
                WHEN 'IE00BYX2JD69' THEN 41.50
                WHEN 'LU1681043599' THEN 12.80
                WHEN 'IE00B4L5YC18' THEN 11.50
                WHEN 'LU0290358497' THEN 28.90
                WHEN 'IE00B1XNHC34' THEN -15.30
                WHEN 'LU0959211243' THEN 65.40
                WHEN 'ES0152743003' THEN 35.60
                ELSE 25.00
            END,
            CASE f.Isin
                WHEN 'IE00B4L5Y983' THEN 78.10
                WHEN 'IE00B5BMR087' THEN 102.50
                WHEN 'LU0996182563' THEN 75.20
                WHEN 'IE00BYX2JD69' THEN 76.80
                WHEN 'LU1681043599' THEN 22.40
                WHEN 'IE00B4L5YC18' THEN 20.10
                WHEN 'LU0290358497' THEN 48.60
                WHEN 'IE00B1XNHC34' THEN -22.80
                WHEN 'LU0959211243' THEN 118.90
                WHEN 'ES0152743003' THEN 62.30
                ELSE 45.00
            END,
            CASE f.Isin
                WHEN 'IE00B4L5Y983' THEN 14.20
                WHEN 'IE00B5BMR087' THEN 15.80
                WHEN 'LU0996182563' THEN 14.50
                WHEN 'IE00BYX2JD69' THEN 14.30
                WHEN 'LU1681043599' THEN 18.90
                WHEN 'IE00B4L5YC18' THEN 19.20
                WHEN 'LU0290358497' THEN 16.40
                WHEN 'IE00B1XNHC34' THEN 28.50
                WHEN 'LU0959211243' THEN 18.10
                WHEN 'ES0152743003' THEN 16.80
                ELSE 15.00
            END,
            CASE f.Isin
                WHEN 'IE00B4L5Y983' THEN 1.30
                WHEN 'IE00B5BMR087' THEN 1.57
                WHEN 'LU0996182563' THEN 1.23
                WHEN 'IE00BYX2JD69' THEN 1.27
                WHEN 'LU1681043599' THEN 0.34
                WHEN 'IE00B4L5YC18' THEN 0.31
                WHEN 'LU0290358497' THEN 0.75
                WHEN 'IE00B1XNHC34' THEN -0.30
                WHEN 'LU0959211243' THEN 1.59
                WHEN 'ES0152743003' THEN 0.92
                ELSE 0.67
            END)
    ) AS perf(Return1Month, Return3Months, Return6Months, Return1Year, Return3Years, Return5Years, Volatility, SharpeRatio);
END
GO

PRINT '003_constraints_and_perf_seed applied successfully.';
GO
