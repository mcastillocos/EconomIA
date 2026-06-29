namespace EconomIA.Infrastructure.Workflows;

/// <summary>
/// Catálogo de workflows predefinidos para el producto de Luis Torras.
/// Cada workflow encadena múltiples agentes para producir análisis completos.
/// </summary>
public static class PredefinedWorkflows
{
    /// <summary>
    /// Morning Briefing Enriquecido:
    /// 1. Obtener noticias relevantes (DailyNewsAgent)
    /// 2. Analizar riesgos de la cartera (RiskAgent)
    /// 3. Generar briefing consolidado (PortfolioBriefingAgent)
    /// </summary>
    public static WorkflowDefinition MorningBriefing => new()
    {
        Name = "morning_briefing",
        Description = "Briefing matutino completo: noticias + riesgos + resumen de cartera",
        Steps =
        [
            new()
            {
                Name = "noticias",
                AgentName = "daily_news",
                OutputAsNextInput = true,
                ExtraContext = { ["focus"] = "portfolio_relevant" },
            },
            new()
            {
                Name = "riesgos",
                AgentName = "risk",
                OutputAsNextInput = false,
                ExtraContext = { ["include_news_context"] = "true" },
            },
            new()
            {
                Name = "briefing_final",
                AgentName = "portfolio_briefing",
                OutputAsNextInput = true,
                InputTransform = (input, ctx) =>
                    $"Genera un briefing consolidado incorporando:\n\nNOTICIAS:\n{ctx.GetValueOrDefault("step_noticias_output", "")}\n\nRIESGOS:\n{ctx.GetValueOrDefault("step_riesgos_output", "")}\n\nInput original: {input}",
            },
        ],
    };

    /// <summary>
    /// Research Completo de Empresa:
    /// 1. Análisis fundamental (CompanyAnalysisAgent)
    /// 2. Datos financieros (FinancialDataExtractorAgent)
    /// 3. Evaluación de riesgo (RiskAgent)
    /// 4. Comparación con competidores (ComparisonAgent)
    /// </summary>
    public static WorkflowDefinition CompanyResearch => new()
    {
        Name = "company_research",
        Description = "Investigación completa de una empresa: fundamental + datos + riesgo + comparativa",
        Steps =
        [
            new()
            {
                Name = "analisis_fundamental",
                AgentName = "company_analysis",
                OutputAsNextInput = true,
            },
            new()
            {
                Name = "datos_y_riesgo",
                Parallel = true,
                ParallelAgents = ["financial_data_extractor", "risk"],
                AgentName = "financial_data_extractor",
                ContinueOnFailure = true,
            },
            new()
            {
                Name = "comparativa",
                AgentName = "comparison",
                InputTransform = (input, ctx) =>
                    $"Compara la empresa del análisis con sus principales competidores.\n\nANÁLISIS PREVIO:\n{ctx.GetValueOrDefault("step_analisis_fundamental_output", "")}\n\nDATOS:\n{ctx.GetValueOrDefault("step_datos_y_riesgo_output", "")}",
            },
        ],
    };

    /// <summary>
    /// Due Diligence de Fondo:
    /// 1. Análisis del fondo (FundAnalysisAgent)
    /// 2. Validación de datos (DataValidationAgent)
    /// 3. Evaluación de riesgo (RiskAgent)
    /// 4. Screening comparativo (ScreenerAgent)
    /// </summary>
    public static WorkflowDefinition FundDueDiligence => new()
    {
        Name = "fund_due_diligence",
        Description = "Due diligence completa de un fondo: análisis + validación + riesgo + screening",
        Steps =
        [
            new()
            {
                Name = "analisis_fondo",
                AgentName = "fund_analysis",
                OutputAsNextInput = true,
            },
            new()
            {
                Name = "validacion_datos",
                AgentName = "data_validation",
                ContinueOnFailure = true,
                ExtraContext = { ["scope"] = "fund_metrics" },
            },
            new()
            {
                Name = "evaluacion_riesgo",
                AgentName = "risk",
                InputTransform = (input, ctx) =>
                    $"Evalúa los riesgos del fondo basándote en:\n\nANÁLISIS:\n{ctx.GetValueOrDefault("step_analisis_fondo_output", "")}\n\nVALIDACIÓN:\n{ctx.GetValueOrDefault("step_validacion_datos_output", "")}",
            },
            new()
            {
                Name = "screening_comparativo",
                AgentName = "screener",
                InputTransform = (input, ctx) =>
                    $"Busca fondos similares o mejores alternativas al fondo analizado.\n\nCONTEXTO:\n{ctx.GetValueOrDefault("step_analisis_fondo_output", "")}",
            },
        ],
    };

    /// <summary>
    /// Análisis de Informe Anual:
    /// 1. Lectura del informe (AnnualReportReaderAgent)
    /// 2. Extracción de datos clave (FinancialDataExtractorAgent)
    /// 3. Análisis de la empresa (CompanyAnalysisAgent)
    /// </summary>
    public static WorkflowDefinition AnnualReportAnalysis => new()
    {
        Name = "annual_report_analysis",
        Description = "Análisis completo de un informe anual: lectura + extracción + análisis",
        Steps =
        [
            new()
            {
                Name = "lectura_informe",
                AgentName = "annual_report_reader",
                OutputAsNextInput = true,
            },
            new()
            {
                Name = "extraccion_datos",
                AgentName = "financial_data_extractor",
                InputTransform = (input, ctx) =>
                    $"Extrae métricas financieras clave del siguiente resumen de informe anual:\n\n{ctx.GetValueOrDefault("step_lectura_informe_output", "")}",
            },
            new()
            {
                Name = "analisis_empresa",
                AgentName = "company_analysis",
                InputTransform = (input, ctx) =>
                    $"Realiza un análisis basado en el informe anual:\n\nRESUMEN:\n{ctx.GetValueOrDefault("step_lectura_informe_output", "")}\n\nDATOS EXTRAÍDOS:\n{ctx.GetValueOrDefault("step_extraccion_datos_output", "")}",
            },
        ],
    };

    /// <summary>
    /// Devuelve todos los workflows disponibles.
    /// </summary>
    public static IReadOnlyList<WorkflowDefinition> All =>
    [
        MorningBriefing,
        CompanyResearch,
        FundDueDiligence,
        AnnualReportAnalysis,
    ];

    /// <summary>
    /// Busca un workflow por nombre.
    /// </summary>
    public static WorkflowDefinition? GetByName(string name) =>
        All.FirstOrDefault(w => w.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
}
