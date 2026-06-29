using EconomIA.Application.DTOs;
using EconomIA.Domain.Entities;
using EconomIA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EconomIA.Infrastructure.ExternalServices;

public class PortfolioOptimizerService
{
    private readonly EconomIADbContext _db;

    public PortfolioOptimizerService(EconomIADbContext db) => _db = db;

    public async Task<OptimizedPortfolio> OptimizeAsync(PortfolioOptimizationRequest request)
    {
        var funds = await _db.Set<Fund>()
            .AsNoTracking()
            .Include(f => f.Performances)
            .Where(f => request.FundIds.Contains(f.Id.ToString()))
            .ToListAsync();

        if (funds.Count < 2)
            throw new InvalidOperationException("Se necesitan al menos 2 fondos para optimizar.");

        // Extraer retornos y volatilidades
        var assets = funds.Select(f =>
        {
            var perf = f.Performances.OrderByDescending(p => p.RecordedAt).FirstOrDefault();
            return new AssetData(
                f,
                perf?.Return1Year.Value ?? 0m,
                perf?.Volatility.Value ?? 10m,
                perf?.SharpeRatio ?? 0m
            );
        }).ToList();

        // Markowitz simplificado: optimización por cuadrícula
        // Genera combinaciones de pesos y encuentra la óptima
        var n = assets.Count;
        var bestWeights = new decimal[n];
        var bestSharpe = decimal.MinValue;
        var bestReturn = 0m;
        var bestRisk = 0m;

        // Para pocos activos: Monte Carlo con 10000 combinaciones aleatorias
        var rng = new Random(42);
        var iterations = Math.Min(50000, (int)Math.Pow(10, Math.Min(n, 5)));

        for (int iter = 0; iter < iterations; iter++)
        {
            // Generar pesos aleatorios normalizados
            var weights = new decimal[n];
            var sum = 0m;
            for (int i = 0; i < n; i++)
            {
                weights[i] = (decimal)rng.NextDouble();
                sum += weights[i];
            }
            for (int i = 0; i < n; i++) weights[i] /= sum;

            // Calcular retorno y riesgo de la cartera
            var portfolioReturn = 0m;
            var portfolioVariance = 0m;

            for (int i = 0; i < n; i++)
            {
                portfolioReturn += weights[i] * assets[i].Return;
                // Diagonal de la varianza (simplificado - asumimos baja correlación)
                portfolioVariance += weights[i] * weights[i] * assets[i].Volatility * assets[i].Volatility;

                // Covarianza estimada (correlación 0.3 entre activos)
                for (int j = i + 1; j < n; j++)
                {
                    portfolioVariance += 2 * weights[i] * weights[j] * 0.3m * assets[i].Volatility * assets[j].Volatility;
                }
            }

            var portfolioRisk = (decimal)Math.Sqrt((double)portfolioVariance);
            var sharpe = portfolioRisk > 0 ? (portfolioReturn - 2m) / portfolioRisk : 0m; // Risk-free = 2%

            if (request.MinimizeRisk)
            {
                // Si queremos minimizar riesgo para un retorno target
                if (portfolioReturn >= request.TargetReturn && sharpe > bestSharpe)
                {
                    bestSharpe = sharpe;
                    bestReturn = portfolioReturn;
                    bestRisk = portfolioRisk;
                    Array.Copy(weights, bestWeights, n);
                }
            }
            else
            {
                // Maximizar Sharpe
                if (sharpe > bestSharpe)
                {
                    bestSharpe = sharpe;
                    bestReturn = portfolioReturn;
                    bestRisk = portfolioRisk;
                    Array.Copy(weights, bestWeights, n);
                }
            }
        }

        // Generar frontera eficiente
        var frontier = GenerateEfficientFrontier(assets, n);

        var portfolioWeights = assets.Select((a, i) => new PortfolioWeight(
            a.Fund.Id.ToString(),
            a.Fund.Name,
            Math.Round(bestWeights[i] * 100, 2),
            a.Return,
            a.Volatility
        )).OrderByDescending(w => w.Weight).ToList();

        return new OptimizedPortfolio(
            portfolioWeights,
            Math.Round(bestReturn, 2),
            Math.Round(bestRisk, 2),
            Math.Round(bestSharpe, 3),
            frontier
        );
    }

    private static List<EfficientFrontierPoint> GenerateEfficientFrontier(
        List<AssetData> assets, int n)
    {
        var frontier = new List<EfficientFrontierPoint>();
        var rng = new Random(123);

        // Generar 5000 portfolios aleatorios para mapear la frontera
        var points = new List<(decimal risk, decimal ret)>();

        for (int iter = 0; iter < 5000; iter++)
        {
            var weights = new decimal[n];
            var sum = 0m;
            for (int i = 0; i < n; i++) { weights[i] = (decimal)rng.NextDouble(); sum += weights[i]; }
            for (int i = 0; i < n; i++) weights[i] /= sum;

            var portfolioReturn = 0m;
            var portfolioVariance = 0m;
            for (int i = 0; i < n; i++)
            {
                portfolioReturn += weights[i] * assets[i].Return;
                var vol = assets[i].Volatility;
                portfolioVariance += weights[i] * weights[i] * vol * vol;
                for (int j = i + 1; j < n; j++)
                {
                    var vol2 = assets[j].Volatility;
                    portfolioVariance += 2 * weights[i] * weights[j] * 0.3m * vol * vol2;
                }
            }
            var risk = (decimal)Math.Sqrt((double)portfolioVariance);
            points.Add((risk, portfolioReturn));
        }

        // Extraer frontera eficiente (máximo retorno por cada nivel de riesgo)
        var grouped = points.GroupBy(p => Math.Round(p.risk, 1))
            .Select(g => new EfficientFrontierPoint(g.Key, g.Max(p => p.ret)))
            .OrderBy(p => p.Risk)
            .ToList();

        return grouped;
    }

    private record AssetData(Fund Fund, decimal Return, decimal Volatility, decimal Sharpe);
}
