namespace EconomIA.Application.DTOs;

public record PortfolioOptimizationRequest(
    List<string> FundIds,
    decimal TargetReturn,
    bool MinimizeRisk = true
);

public record OptimizedPortfolio(
    List<PortfolioWeight> Weights,
    decimal ExpectedReturn,
    decimal ExpectedRisk,
    decimal SharpeRatio,
    List<EfficientFrontierPoint> EfficientFrontier
);

public record PortfolioWeight(
    string FundId,
    string FundName,
    decimal Weight,
    decimal Return1Y,
    decimal Volatility
);

public record EfficientFrontierPoint(
    decimal Risk,
    decimal Return
);
