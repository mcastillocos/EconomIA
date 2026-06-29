using EconomIA.Domain.Entities;

namespace EconomIA.Infrastructure.Connectors;

/// <summary>
/// Templates de checklist predefinidos para análisis de inversiones.
/// Luis puede usarlos directamente o como base para crear los suyos.
/// </summary>
public static class PredefinedChecklists
{
    public static ChecklistTemplate CompanyDueDiligence()
    {
        var t = ChecklistTemplate.Create("Due Diligence Empresa", "company",
            "Checklist completo para evaluar una empresa antes de invertir", isBuiltIn: true);

        // Negocio
        t.AddItem("¿Entiendo el modelo de negocio?", "Negocio", 1, "boolean", "¿Puedes explicar cómo gana dinero en 2 frases?");
        t.AddItem("¿Tiene ventaja competitiva sostenible (moat)?", "Negocio", 2, "boolean", "Marca, red, costes de cambio, patentes, escala");
        t.AddItem("¿El mercado total direccionable (TAM) es grande y creciente?", "Negocio", 3, "boolean");
        t.AddItem("¿Tiene poder de fijación de precios?", "Negocio", 4, "boolean");
        t.AddItem("Calidad del equipo directivo", "Negocio", 5, "rating", "1=Pobre, 5=Excelente");

        // Financiero
        t.AddItem("¿Revenue creciendo >5% anual últimos 5 años?", "Financiero", 6, "boolean");
        t.AddItem("¿Márgenes brutos estables o mejorando?", "Financiero", 7, "boolean");
        t.AddItem("¿Genera free cash flow positivo consistente?", "Financiero", 8, "boolean");
        t.AddItem("¿Deuda/Equity < 1.0?", "Financiero", 9, "boolean");
        t.AddItem("¿ROE > 15%?", "Financiero", 10, "boolean");
        t.AddItem("¿ROIC > WACC?", "Financiero", 11, "boolean");

        // Valoración
        t.AddItem("¿PER razonable vs histórico y sector?", "Valoración", 12, "boolean");
        t.AddItem("¿EV/EBITDA atractivo vs peers?", "Valoración", 13, "boolean");
        t.AddItem("¿FCF Yield > 5%?", "Valoración", 14, "boolean");
        t.AddItem("Margen de seguridad estimado (%)", "Valoración", 15, "numeric");

        // Riesgos
        t.AddItem("¿Riesgo regulatorio significativo?", "Riesgos", 16, "boolean");
        t.AddItem("¿Concentración de clientes preocupante?", "Riesgos", 17, "boolean");
        t.AddItem("¿Riesgo tecnológico/disrupción?", "Riesgos", 18, "boolean");
        t.AddItem("Notas sobre riesgos identificados", "Riesgos", 19, "text");

        // Decisión
        t.AddItem("Tesis de inversión en 3 frases", "Decisión", 20, "text");
        t.AddItem("Puntuación global", "Decisión", 21, "rating", "1=No invertir, 5=Alta convicción");

        return t;
    }

    public static ChecklistTemplate FundEvaluation()
    {
        var t = ChecklistTemplate.Create("Evaluación de Fondo", "fund",
            "Checklist para evaluar un fondo de inversión", isBuiltIn: true);

        t.AddItem("¿Rendimiento 3-5 años superior a benchmark?", "Rendimiento", 1, "boolean");
        t.AddItem("¿Volatilidad aceptable para mi perfil?", "Rendimiento", 2, "boolean");
        t.AddItem("¿Sharpe ratio > 1.0?", "Rendimiento", 3, "boolean");
        t.AddItem("¿Max drawdown asumible?", "Rendimiento", 4, "boolean");

        t.AddItem("¿TER < media de la categoría?", "Costes", 5, "boolean");
        t.AddItem("¿Sin comisión de éxito abusiva?", "Costes", 6, "boolean");
        t.AddItem("TER del fondo (%)", "Costes", 7, "numeric");

        t.AddItem("¿Gestor con track record > 5 años?", "Gestión", 8, "boolean");
        t.AddItem("¿Gestora de confianza y solvente?", "Gestión", 9, "boolean");
        t.AddItem("¿Proceso de inversión claro y replicable?", "Gestión", 10, "boolean");
        t.AddItem("Calidad de la gestión", "Gestión", 11, "rating");

        t.AddItem("¿Diversificación geográfica adecuada?", "Cartera", 12, "boolean");
        t.AddItem("¿Concentración sectorial aceptable?", "Cartera", 13, "boolean");
        t.AddItem("¿Liquidez del fondo suficiente?", "Cartera", 14, "boolean");

        t.AddItem("Notas adicionales", "Decisión", 15, "text");
        t.AddItem("Puntuación global", "Decisión", 16, "rating", "1=Evitar, 5=Comprar");

        return t;
    }

    public static ChecklistTemplate AnnualReportReview()
    {
        var t = ChecklistTemplate.Create("Revisión Informe Anual", "company",
            "Checklist para revisar un informe anual", isBuiltIn: true);

        t.AddItem("¿Revenue vs guidance del año anterior?", "Resultados", 1, "boolean");
        t.AddItem("¿Márgenes operativos en línea o mejorando?", "Resultados", 2, "boolean");
        t.AddItem("¿Cash flow operativo creciente?", "Resultados", 3, "boolean");
        t.AddItem("¿Guidance para próximo año positivo?", "Resultados", 4, "boolean");

        t.AddItem("¿Deuda a largo plazo controlada?", "Balance", 5, "boolean");
        t.AddItem("¿Working capital positivo?", "Balance", 6, "boolean");
        t.AddItem("¿Goodwill < 30% de activos totales?", "Balance", 7, "boolean");

        t.AddItem("¿Carta del CEO relevante y honesta?", "Governance", 8, "boolean");
        t.AddItem("¿Compensación directiva alineada con accionistas?", "Governance", 9, "boolean");
        t.AddItem("¿Riesgos bien documentados?", "Governance", 10, "boolean");

        t.AddItem("Hallazgos clave", "Resumen", 11, "text");
        t.AddItem("¿Cambió mi tesis?", "Resumen", 12, "boolean");

        return t;
    }

    public static IReadOnlyList<ChecklistTemplate> All => [CompanyDueDiligence(), FundEvaluation(), AnnualReportReview()];
}
