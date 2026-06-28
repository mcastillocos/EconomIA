using EconomIA.Application.Interfaces;
using EconomIA.Domain.Ports;
using Microsoft.Extensions.DependencyInjection;

namespace EconomIA.Infrastructure.ExternalServices.Agents;

public class CompanyAnalysisAgent : IAgent
{
    private readonly IServiceProvider _serviceProvider;

    public CompanyAnalysisAgent(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public string Name => "CompanyAnalysisAgent";
    public string Description => "Análisis fundamental completo de una empresa con datos disponibles";

    public async Task<AgentOutput> ExecuteAsync(ILlmService llm, string input, Dictionary<string, string> context, CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var metricRepo = scope.ServiceProvider.GetRequiredService<IFinancialMetricRepository>();
        var companyRepo = scope.ServiceProvider.GetRequiredService<ICompanyRepository>();

        // Buscar empresa por nombre o ticker
        var companies = await companyRepo.GetAllAsync(ct);
        var company = companies.FirstOrDefault(c =>
            c.Name.Contains(input, StringComparison.OrdinalIgnoreCase) ||
            (c.Ticker?.Contains(input, StringComparison.OrdinalIgnoreCase) ?? false));

        var dataContext = "";
        var sources = new List<string>();

        if (company is not null)
        {
            dataContext += $"Empresa: {company.Name}\nTicker: {company.Ticker}\nISIN: {company.Isin}\nSector: {company.Sector}\nPaís: {company.Country}\nIndustria: {company.Industry}\n\n";

            // Obtener métricas financieras
            var metrics = await metricRepo.GetByEntityAsync("company", company.Id, ct);
            if (metrics.Count > 0)
            {
                dataContext += "DATOS FINANCIEROS DISPONIBLES:\n";
                foreach (var m in metrics.Take(50))
                {
                    dataContext += $"- {m.MetricName}: {m.Value}{(m.Currency is not null ? $" {m.Currency}" : "")} ({m.Year}{(m.Quarter.HasValue ? $"/Q{m.Quarter}" : "")}) [Fuente: {m.Source ?? m.FileName ?? "manual"}, Confianza: {m.Confidence}]\n";
                    sources.Add($"{m.MetricName}: {m.Source ?? m.FileName ?? "dato cargado"}");
                }
            }
            else
            {
                dataContext += "No hay datos financieros cargados para esta empresa.\n";
            }

            if (company.Notes is not null)
                dataContext += $"\nNotas del usuario: {company.Notes}\n";
        }
        else
        {
            dataContext = $"No se encontró la empresa '{input}' en la base de datos. Analizando solo con el nombre proporcionado.\n";
        }

        var systemPrompt = @"Eres un analista financiero especializado en small y micro caps. Tu trabajo es analizar empresas de forma rigurosa.

REGLAS ESTRICTAS:
- Solo usa datos que se te proporcionan. NUNCA inventes cifras.
- Si falta información, indica explícitamente qué datos faltan.
- No hagas recomendaciones de compra/venta.
- Cita la fuente de cada dato que uses.
- Diferencia claramente entre datos verificados e inferencias.
- Si no tienes datos suficientes, dilo claramente.

CHECKLIST DE ANÁLISIS:
1. Modelo de negocio
2. Crecimiento
3. Márgenes
4. Deuda y caja
5. Generación de caja
6. ROIC / ROE
7. Valoración (PER, EV/EBITDA, FCF yield)
8. Riesgos identificados
9. Guidance (si disponible)
10. Puntos a vigilar

Responde en español. Formato Markdown.";

        var userPrompt = $"Analiza la siguiente empresa:\n\n{dataContext}\n\nRealiza el análisis siguiendo la checklist. Para cada punto, indica si tienes datos o si faltan.";

        var response = await llm.ChatAsync(systemPrompt, userPrompt, new LlmOptions { MaxTokens = 4000, Temperature = 0.2 }, ct);

        return new AgentOutput
        {
            Output = response.Content,
            Sources = sources.Count > 0 ? string.Join("\n", sources.Distinct()) : "Sin datos cargados — análisis basado solo en nombre"
        };
    }
}

public class FundAnalysisAgent : IAgent
{
    private readonly IServiceProvider _serviceProvider;

    public FundAnalysisAgent(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public string Name => "FundAnalysisAgent";
    public string Description => "Análisis completo de un fondo de inversión con datos disponibles";

    public async Task<AgentOutput> ExecuteAsync(ILlmService llm, string input, Dictionary<string, string> context, CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var fundRepo = scope.ServiceProvider.GetRequiredService<IFundRepository>();
        var metricRepo = scope.ServiceProvider.GetRequiredService<IFinancialMetricRepository>();

        var funds = await fundRepo.GetTopFundsAsync(200, ct);
        var fund = funds.FirstOrDefault(f =>
            f.Name.Contains(input, StringComparison.OrdinalIgnoreCase) ||
            f.Isin.Value.Contains(input, StringComparison.OrdinalIgnoreCase));

        var dataContext = "";
        var sources = new List<string>();

        if (fund is not null)
        {
            dataContext += $"Fondo: {fund.Name}\nISIN: {fund.Isin.Value}\nCategoría: {fund.Category}\nGestora: {fund.ManagementCompany}\n";
            dataContext += $"Riesgo: {fund.RiskLevel}\nVL: {fund.NetAssetValue.Amount} {fund.NetAssetValue.Currency}\n";
            dataContext += $"TER: {fund.ExpenseRatio.Value}%\nRating: {fund.Rating}\nRanking: #{fund.RankingPosition}\n\n";

            var latestPerf = fund.Performances.OrderByDescending(p => p.RecordedAt).FirstOrDefault();
            if (latestPerf is not null)
            {
                dataContext += "RENDIMIENTO:\n";
                dataContext += $"- 1 mes: {latestPerf.Return1Month.Value}%\n";
                dataContext += $"- 3 meses: {latestPerf.Return3Months.Value}%\n";
                dataContext += $"- 6 meses: {latestPerf.Return6Months.Value}%\n";
                dataContext += $"- 1 año: {latestPerf.Return1Year.Value}%\n";
                dataContext += $"- 3 años: {latestPerf.Return3Years.Value}%\n";
                dataContext += $"- 5 años: {latestPerf.Return5Years.Value}%\n";
                dataContext += $"- Volatilidad: {latestPerf.Volatility.Value}%\n";
                dataContext += $"- Sharpe Ratio: {latestPerf.SharpeRatio}\n\n";
                sources.Add("Performance data: Base de datos EconomIA");
            }

            // Métricas adicionales
            var metrics = await metricRepo.GetByEntityAsync("fund", fund.Id, ct);
            if (metrics.Count > 0)
            {
                dataContext += "DATOS ADICIONALES:\n";
                foreach (var m in metrics.Take(30))
                {
                    dataContext += $"- {m.MetricName}: {m.Value}{(m.Currency is not null ? $" {m.Currency}" : "")} [Fuente: {m.Source ?? "cargado"}]\n";
                    sources.Add($"{m.MetricName}: {m.Source ?? m.FileName ?? "dato cargado"}");
                }
            }
        }
        else
        {
            dataContext = $"No se encontró el fondo '{input}' en la base de datos.\n";
        }

        var systemPrompt = @"Eres un analista de fondos de inversión. Analiza fondos de forma rigurosa.

REGLAS ESTRICTAS:
- Solo usa datos proporcionados. NUNCA inventes cifras.
- Si falta información, indícalo.
- No recomiendes compra/venta.
- Cita fuentes de cada dato.

CHECKLIST DE ANÁLISIS DE FONDOS:
1. Rentabilidad (absoluta y relativa a categoría)
2. Volatilidad y riesgo
3. Drawdown máximo (si disponible)
4. Comisiones (TER)
5. Categoría y benchmark
6. Composición (si disponible)
7. Principales posiciones (si disponible)
8. Consistencia del gestor
9. Comparación con alternativas
10. Puntos a vigilar

Responde en español. Formato Markdown.";

        var userPrompt = $"Analiza el siguiente fondo:\n\n{dataContext}\n\nRealiza el análisis siguiendo la checklist.";

        var response = await llm.ChatAsync(systemPrompt, userPrompt, new LlmOptions { MaxTokens = 4000, Temperature = 0.2 }, ct);

        return new AgentOutput
        {
            Output = response.Content,
            Sources = sources.Count > 0 ? string.Join("\n", sources.Distinct()) : "Sin datos adicionales cargados"
        };
    }
}
