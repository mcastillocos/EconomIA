using EconomIA.Application.Interfaces;
using EconomIA.Domain.Ports;
using Microsoft.Extensions.DependencyInjection;

namespace EconomIA.Infrastructure.ExternalServices.Agents;

/// <summary>
/// Extrae puntos clave de earnings calls (transcripciones cargadas como documentos).
/// </summary>
public class EarningsCallAgent : IAgent
{
    private readonly IServiceProvider _serviceProvider;

    public EarningsCallAgent(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public string Name => "EarningsCallAgent";
    public string Description => "Destilación de earnings calls: guidance, sorpresas, cambios de tono";

    public async Task<AgentOutput> ExecuteAsync(ILlmService llm, string input, Dictionary<string, string> context, CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var docRepo = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
        var companyRepo = scope.ServiceProvider.GetRequiredService<ICompanyRepository>();

        // Buscar documentos tipo transcript
        var docs = await docRepo.GetAllAsync(ct);
        var transcripts = docs
            .Where(d => d.FileName.Contains("earning", StringComparison.OrdinalIgnoreCase)
                     || d.FileName.Contains("transcript", StringComparison.OrdinalIgnoreCase)
                     || d.FileName.Contains("call", StringComparison.OrdinalIgnoreCase)
                     || (d.FileName.Contains(input, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(d => d.UploadDate)
            .Take(3)
            .ToList();

        var dataContext = "";
        var sources = new List<string>();

        if (transcripts.Count > 0)
        {
            dataContext += "TRANSCRIPCIONES ENCONTRADAS:\n";
            foreach (var t in transcripts)
            {
                dataContext += $"\n--- {t.FileName} (subido: {t.UploadDate:yyyy-MM-dd}) ---\n";
                if (t.ExtractedText is not null)
                {
                    // Limitar a 3000 chars por transcript para no exceder contexto
                    var content = t.ExtractedText.Length > 3000
                        ? t.ExtractedText[..3000] + "\n[...truncado...]"
                        : t.ExtractedText;
                    dataContext += content + "\n";
                }
                else
                {
                    dataContext += "[Sin contenido procesado - se necesita re-procesar el documento]\n";
                }
                sources.Add($"Transcript: {t.FileName}");
            }
        }
        else
        {
            dataContext = $"No se encontraron transcripciones de earnings calls para '{input}'.\nSube un archivo con la transcripción en la sección Uploads.\n";
        }

        var systemPrompt = @"Eres un analista especializado en earnings calls. Tu trabajo es destilar la información clave de transcripciones de conferencias de resultados.

REGLAS:
- Extrae SOLO lo que dice la transcripción. NO inventes datos.
- Si la transcripción está truncada o incompleta, indícalo.

CHECKLIST DE EXTRACCIÓN:
1. **Guidance**: ¿Qué espera el management para próximos trimestres?
2. **Sorpresas vs consenso**: ¿Qué fue mejor/peor de lo esperado?
3. **Cambios de tono**: ¿El management suena más/menos optimista que antes?
4. **Segmentos clave**: Revenue por segmento si se menciona
5. **Capital allocation**: Dividendos, recompras, M&A, capex
6. **Riesgos mencionados**: ¿Qué preocupa al management?
7. **Preguntas de analistas**: ¿Qué preguntaron? ¿Qué evadieron?
8. **Palabras clave**: Términos repetidos o enfatizados

Responde en español. Formato Markdown con secciones claras.";

        var userPrompt = $"Analiza la(s) earnings call(s) de: {input}\n\n{dataContext}";

        var response = await llm.ChatAsync(systemPrompt, userPrompt, new LlmOptions { MaxTokens = 4000, Temperature = 0.2 }, ct);

        return new AgentOutput
        {
            Output = response.Content,
            Sources = sources.Count > 0 ? string.Join("\n", sources) : "Sin transcripciones disponibles"
        };
    }
}

/// <summary>
/// Lee informes anuales y extrae datos clave con checklist estructurado.
/// </summary>
public class AnnualReportReaderAgent : IAgent
{
    private readonly IServiceProvider _serviceProvider;

    public AnnualReportReaderAgent(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public string Name => "AnnualReportReaderAgent";
    public string Description => "Lectura de informes anuales con checklist estructurado";

    public async Task<AgentOutput> ExecuteAsync(ILlmService llm, string input, Dictionary<string, string> context, CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var docRepo = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();

        var docs = await docRepo.GetAllAsync(ct);
        var reports = docs
            .Where(d => d.FileName.Contains("annual", StringComparison.OrdinalIgnoreCase)
                     || d.FileName.Contains("informe", StringComparison.OrdinalIgnoreCase)
                     || d.FileName.Contains("report", StringComparison.OrdinalIgnoreCase)
                     || d.FileName.Contains(input, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(d => d.UploadDate)
            .Take(2)
            .ToList();

        var dataContext = "";
        var sources = new List<string>();

        if (reports.Count > 0)
        {
            dataContext += "DOCUMENTOS ENCONTRADOS:\n";
            foreach (var r in reports)
            {
                dataContext += $"\n--- {r.FileName} ({r.UploadDate:yyyy-MM-dd}) ---\n";
                if (r.ExtractedText is not null)
                {
                    var content = r.ExtractedText.Length > 5000
                        ? r.ExtractedText[..5000] + "\n[...truncado...]"
                        : r.ExtractedText;
                    dataContext += content + "\n";
                }
                else
                {
                    dataContext += "[Sin contenido procesado]\n";
                }
                sources.Add($"Informe: {r.FileName}");
            }
        }
        else
        {
            dataContext = $"No se encontraron informes anuales para '{input}'.\nSube el PDF/documento en la sección Uploads.\n";
        }

        var systemPrompt = @"Eres un analista especializado en leer informes anuales corporativos. Extraes datos clave de forma estructurada.

REGLAS:
- Solo usa datos del documento proporcionado. NUNCA inventes cifras.
- Si el documento está truncado, indica qué secciones faltan.

CHECKLIST DE EXTRACCIÓN DE INFORME ANUAL:
1. **Resumen del ejercicio**: Revenue, EBITDA, Beneficio neto, FCF
2. **Crecimiento YoY**: Variación vs año anterior
3. **Márgenes**: Bruto, EBITDA, Neto
4. **Balance**: Deuda neta, Equity, DFN/EBITDA
5. **Cash Flow**: Operativo, Inversión, Financiación
6. **Dividendos**: Payout, DPS, yield
7. **Guidance/Outlook**: Objetivos para próximo ejercicio
8. **Segmentos**: Desglose por geografía/línea de negocio
9. **Riesgos**: Mencionados por la compañía
10. **Plantilla**: Empleados, cambios organizativos
11. **ESG**: Métricas sostenibilidad si las hay
12. **Cambios contables**: Notas relevantes

Para cada punto: dato + página/sección si disponible.
Responde en español. Formato Markdown.";

        var userPrompt = $"Extrae la información del informe anual de: {input}\n\n{dataContext}";

        var response = await llm.ChatAsync(systemPrompt, userPrompt, new LlmOptions { MaxTokens = 4000, Temperature = 0.1 }, ct);

        return new AgentOutput
        {
            Output = response.Content,
            Sources = sources.Count > 0 ? string.Join("\n", sources) : "Sin informes disponibles"
        };
    }
}

/// <summary>
/// Valida datos financieros cargados: detecta anomalías, inconsistencias, outliers.
/// </summary>
public class DataValidationAgent : IAgent
{
    private readonly IServiceProvider _serviceProvider;

    public DataValidationAgent(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public string Name => "DataValidationAgent";
    public string Description => "Validación automática de datos financieros: anomalías, inconsistencias, outliers";

    public async Task<AgentOutput> ExecuteAsync(ILlmService llm, string input, Dictionary<string, string> context, CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var metricRepo = scope.ServiceProvider.GetRequiredService<IFinancialMetricRepository>();
        var companyRepo = scope.ServiceProvider.GetRequiredService<ICompanyRepository>();

        var metrics = await metricRepo.GetAllAsync(ct);
        var companies = await companyRepo.GetAllAsync(ct);

        // Filtrar por empresa si se indica
        IReadOnlyList<Domain.Entities.FinancialMetric> targetMetrics;
        if (!string.IsNullOrWhiteSpace(input))
        {
            var company = companies.FirstOrDefault(c =>
                c.Name.Contains(input, StringComparison.OrdinalIgnoreCase) ||
                (c.Ticker?.Contains(input, StringComparison.OrdinalIgnoreCase) ?? false));

            targetMetrics = company is not null
                ? metrics.Where(m => m.EntityId == company.Id).ToList()
                : metrics;
        }
        else
        {
            targetMetrics = metrics;
        }

        var dataContext = $"DATOS A VALIDAR ({targetMetrics.Count} métricas):\n\n";
        var sources = new List<string>();

        if (targetMetrics.Count > 0)
        {
            // Agrupar por empresa/entidad
            var grouped = targetMetrics
                .GroupBy(m => new { m.EntityType, m.EntityId })
                .Take(10);

            foreach (var g in grouped)
            {
                var entityName = companies.FirstOrDefault(c => c.Id == g.Key.EntityId)?.Name ?? g.Key.EntityType;
                dataContext += $"\n[{entityName}]:\n";
                foreach (var m in g.OrderBy(m => m.MetricName).ThenBy(m => m.Year).Take(30))
                {
                    dataContext += $"  {m.MetricName}: {m.Value}{(m.Currency is not null ? $" {m.Currency}" : "")} | Año: {m.Year}{(m.Quarter.HasValue ? $"/Q{m.Quarter}" : "")} | Fuente: {m.Source ?? m.FileName ?? "manual"} | Confianza: {m.Confidence} | Validado: {m.Validated}\n";
                }
                sources.Add($"{entityName}: {g.Count()} métricas");
            }
        }
        else
        {
            dataContext += "No hay métricas cargadas para validar.\n";
        }

        var systemPrompt = @"Eres un agente de control de calidad de datos financieros. Tu trabajo es detectar problemas en los datos cargados.

REGLAS:
- Analiza coherencia entre métricas (ej: Revenue > EBITDA > Net Income en circunstancias normales)
- Detecta outliers (valores que parecen errores de carga: ej. Revenue en millones vs miles)
- Verifica consistencia temporal (ej: crecimiento anómalo sin explicación)
- Comprueba que las unidades/monedas son coherentes
- Identifica datos duplicados o contradictorios
- Marca datos con baja confianza que deberían verificarse

FORMATO DE SALIDA:
1. **Resumen**: X datos OK, Y con problemas potenciales
2. **Errores críticos**: Datos claramente incorrectos
3. **Advertencias**: Datos sospechosos que necesitan verificación
4. **Datos faltantes**: Métricas esperadas que no están
5. **Recomendaciones**: Qué datos añadir para completar el análisis

NO inventes datos. Solo analiza lo proporcionado.
Responde en español. Formato Markdown.";

        var userPrompt = $"Valida los siguientes datos financieros:\n\n{dataContext}";

        var response = await llm.ChatAsync(systemPrompt, userPrompt, new LlmOptions { MaxTokens = 3500, Temperature = 0.1 }, ct);

        return new AgentOutput
        {
            Output = response.Content,
            Sources = sources.Count > 0 ? string.Join("\n", sources) : "Sin datos para validar"
        };
    }
}

/// <summary>
/// Compara fondos o empresas entre sí.
/// </summary>
public class ComparisonAgent : IAgent
{
    private readonly IServiceProvider _serviceProvider;

    public ComparisonAgent(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public string Name => "ComparisonAgent";
    public string Description => "Comparativa entre fondos y/o empresas";

    public async Task<AgentOutput> ExecuteAsync(ILlmService llm, string input, Dictionary<string, string> context, CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var companyRepo = scope.ServiceProvider.GetRequiredService<ICompanyRepository>();
        var fundRepo = scope.ServiceProvider.GetRequiredService<IFundRepository>();
        var metricRepo = scope.ServiceProvider.GetRequiredService<IFinancialMetricRepository>();

        var companies = await companyRepo.GetAllAsync(ct);
        var funds = await fundRepo.GetTopFundsAsync(100, ct);
        var metrics = await metricRepo.GetAllAsync(ct);

        // Intentar encontrar las entidades mencionadas
        var terms = input.Split(new[] { " vs ", " VS ", " contra ", ",", " y " }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var dataContext = "ENTIDADES A COMPARAR:\n\n";
        var sources = new List<string>();

        foreach (var term in terms)
        {
            // Buscar como empresa
            var company = companies.FirstOrDefault(c =>
                c.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (c.Ticker?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));

            if (company is not null)
            {
                dataContext += $"\n## {company.Name} ({company.Ticker ?? "?"})\n";
                dataContext += $"Sector: {company.Sector ?? "?"} | País: {company.Country ?? "?"}\n";
                var compMetrics = metrics.Where(m => m.EntityId == company.Id).OrderByDescending(m => m.Year).Take(20).ToList();
                if (compMetrics.Count > 0)
                {
                    dataContext += "Métricas:\n";
                    foreach (var m in compMetrics)
                        dataContext += $"  - {m.MetricName}: {m.Value}{(m.Currency is not null ? $" {m.Currency}" : "")} ({m.Year})\n";
                }
                sources.Add($"Empresa: {company.Name}");
                continue;
            }

            // Buscar como fondo
            var fund = funds.FirstOrDefault(f =>
                f.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                f.Isin.Value.Contains(term, StringComparison.OrdinalIgnoreCase));

            if (fund is not null)
            {
                dataContext += $"\n## {fund.Name} ({fund.Isin.Value})\n";
                dataContext += $"Cat: {fund.Category} | TER: {fund.ExpenseRatio.Value}% | Riesgo: {fund.RiskLevel}\n";
                var perf = fund.Performances.OrderByDescending(p => p.RecordedAt).FirstOrDefault();
                if (perf is not null)
                {
                    dataContext += $"1M: {perf.Return1Month.Value}% | 1Y: {perf.Return1Year.Value}% | 3Y: {perf.Return3Years.Value}% | Sharpe: {perf.SharpeRatio}\n";
                }
                sources.Add($"Fondo: {fund.Name}");
                continue;
            }

            dataContext += $"\n## {term}\n[No encontrado en base de datos]\n";
        }

        var systemPrompt = @"Eres un analista comparativo. Comparas fondos y/o empresas de forma objetiva.

REGLAS:
- Solo compara con datos proporcionados. NO inventes.
- Si falta información de una entidad, indícalo.
- Usa tabla comparativa cuando sea posible.
- NO recomiendes compra/venta.

FORMATO:
1. Resumen ejecutivo (1-2 líneas)
2. Tabla comparativa de métricas clave
3. Ventajas/desventajas de cada uno
4. Datos que faltan para una comparación completa

Responde en español. Formato Markdown.";

        var userPrompt = $"Compara: {input}\n\n{dataContext}";

        var response = await llm.ChatAsync(systemPrompt, userPrompt, new LlmOptions { MaxTokens = 3500, Temperature = 0.2 }, ct);

        return new AgentOutput
        {
            Output = response.Content,
            Sources = sources.Count > 0 ? string.Join("\n", sources) : "Sin datos para comparar"
        };
    }
}
