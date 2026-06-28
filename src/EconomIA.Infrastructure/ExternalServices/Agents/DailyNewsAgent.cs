using EconomIA.Application.Interfaces;
using EconomIA.Domain.Ports;
using Microsoft.Extensions.DependencyInjection;

namespace EconomIA.Infrastructure.ExternalServices.Agents;

public class DailyNewsAgent : IAgent
{
    private readonly IServiceProvider _serviceProvider;
    public DailyNewsAgent(IServiceProvider serviceProvider) { _serviceProvider = serviceProvider; }
    public string Name => "DailyNewsAgent";
    public string Description => "Briefing diario de noticias relevantes del mercado";

    public async Task<AgentOutput> ExecuteAsync(ILlmService llm, string input, Dictionary<string, string> context, CancellationToken ct)
    {
        var watchlistRepo = _serviceProvider.GetRequiredService<IWatchlistRepository>();
        var companyRepo = _serviceProvider.GetRequiredService<ICompanyRepository>();

        var watchlists = await watchlistRepo.GetAllAsync(ct);
        var companies = await companyRepo.GetAllAsync(ct);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("## Contexto del usuario:");
        sb.AppendLine("### Watchlists:");
        foreach (var w in watchlists)
        {
            sb.AppendLine($"- {w.Name} ({w.Items.Count} elementos)");
        }

        sb.AppendLine("### Empresas seguidas:");
        foreach (var c in companies.Take(20))
        {
            sb.AppendLine($"- {c.Name} ({c.Ticker}) - Sector: {c.Sector}");
        }

        var systemPrompt = """
            Eres un analista de mercados que prepara un briefing diario personalizado.
            Basándote en el contexto del usuario (sus watchlists y empresas seguidas), genera un briefing estructurado:
            
            1. **Resumen del mercado**: Situación general de los principales índices
            2. **Movimientos sectoriales**: Sectores relevantes para el usuario
            3. **Noticias relevantes para la cartera**: Noticias que afectan a las empresas/sectores del usuario
            4. **Eventos hoy/esta semana**: Publicaciones de resultados, datos macro, reuniones de bancos centrales
            5. **Qué vigilar**: Alertas y puntos de atención
            
            IMPORTANTE: Genera el briefing basándote en tu conocimiento general del mercado y personalízalo según lo que sigue el usuario.
            Responde en español con formato Markdown.
            """;

        var userPrompt = string.IsNullOrWhiteSpace(input)
            ? $"Genera el briefing diario para hoy basándote en este contexto:\n\n{sb}"
            : $"Genera el briefing diario enfocado en: {input}\n\nContexto del usuario:\n{sb}";

        var response = await llm.ChatAsync(
            systemPrompt,
            userPrompt,
            new LlmOptions { Temperature = 0.3, MaxTokens = 3500 },
            ct);

        return new AgentOutput { Output = response.Content, Sources = "Watchlists y empresas del usuario" };
    }
}
