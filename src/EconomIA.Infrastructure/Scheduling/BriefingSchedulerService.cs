using EconomIA.Application.Interfaces;
using EconomIA.Infrastructure.Connectors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EconomIA.Infrastructure.Scheduling;

/// <summary>
/// Servicio de scheduling que ejecuta briefings automáticos (DailyNewsAgent + PortfolioBriefingAgent)
/// según un horario configurable. Por defecto: 7:00 AM UTC cada día laborable.
/// </summary>
public class BriefingSchedulerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BriefingSchedulerService> _logger;
    private readonly BriefingSchedulerOptions _options;

    public BriefingSchedulerService(
        IServiceProvider serviceProvider,
        ILogger<BriefingSchedulerService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = configuration.GetSection("BriefingScheduler").Get<BriefingSchedulerOptions>() ?? new();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("BriefingScheduler deshabilitado por configuración");
            return;
        }

        _logger.LogInformation("BriefingScheduler iniciado. Hora programada: {Hour}:{Minute:D2} UTC, solo laborables: {WorkdaysOnly}",
            _options.ScheduleHourUtc, _options.ScheduleMinuteUtc, _options.WorkdaysOnly);

        // Espera inicial para dejar arrancar la app
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var nextRun = CalculateNextRun();
            var delay = nextRun - DateTime.UtcNow;

            if (delay > TimeSpan.Zero)
            {
                _logger.LogInformation("Próximo briefing programado para {NextRun:yyyy-MM-dd HH:mm} UTC (en {Delay:hh\\:mm})",
                    nextRun, delay);
                await Task.Delay(delay, stoppingToken);
            }

            if (stoppingToken.IsCancellationRequested) break;

            await ExecuteBriefingCycleAsync(stoppingToken);
        }
    }

    private async Task ExecuteBriefingCycleAsync(CancellationToken ct)
    {
        _logger.LogInformation("=== Ejecutando ciclo de briefing diario ===");

        using var scope = _serviceProvider.CreateScope();
        var agentService = scope.ServiceProvider.GetRequiredService<IAgentService>();

        // 1. Obtener noticias frescas via RSS
        var newsContext = await FetchNewsContextAsync(scope.ServiceProvider, ct);

        // 2. Ejecutar DailyNewsAgent con noticias reales
        try
        {
            var newsResult = await agentService.RunAgentAsync(
                "daily_news",
                "Genera el briefing diario con las últimas noticias del mercado",
                new Dictionary<string, string>
                {
                    ["news_context"] = newsContext,
                    ["automated"] = "true",
                    ["schedule_run"] = DateTime.UtcNow.ToString("O"),
                },
                ct);

            _logger.LogInformation("DailyNewsAgent completado. Status: {Status}, Longitud: {Length}",
                newsResult.Status, newsResult.Output.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ejecutando DailyNewsAgent en ciclo programado");
        }

        // 3. Ejecutar PortfolioBriefingAgent
        try
        {
            var portfolioResult = await agentService.RunAgentAsync(
                "portfolio_briefing",
                "Genera el resumen ejecutivo de la cartera con los datos actuales",
                new Dictionary<string, string>
                {
                    ["automated"] = "true",
                    ["schedule_run"] = DateTime.UtcNow.ToString("O"),
                },
                ct);

            _logger.LogInformation("PortfolioBriefingAgent completado. Status: {Status}", portfolioResult.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ejecutando PortfolioBriefingAgent en ciclo programado");
        }

        _logger.LogInformation("=== Ciclo de briefing completado ===");
    }

    private async Task<string> FetchNewsContextAsync(IServiceProvider sp, CancellationToken ct)
    {
        try
        {
            var newsConnector = sp.GetService<RssNewsConnector>();
            if (newsConnector is null)
            {
                _logger.LogWarning("RssNewsConnector no disponible, briefing sin contexto de noticias");
                return "No hay noticias disponibles en este momento.";
            }

            var news = await newsConnector.FetchLatestNewsAsync(maxPerFeed: 10, ct: ct);

            if (news.Count == 0)
                return "No se pudieron obtener noticias de los feeds RSS.";

            // Formato conciso para el prompt del agente
            var lines = news.Take(30).Select(n =>
                $"[{n.PublishedAt:dd/MM HH:mm}] [{n.Source}] {n.Title}");

            return $"Últimas noticias financieras ({news.Count} items):\n" + string.Join("\n", lines);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error obteniendo noticias para briefing");
            return "Error obteniendo noticias: " + ex.Message;
        }
    }

    private DateTime CalculateNextRun()
    {
        var now = DateTime.UtcNow;
        var today = now.Date.AddHours(_options.ScheduleHourUtc).AddMinutes(_options.ScheduleMinuteUtc);

        var candidate = now < today ? today : today.AddDays(1);

        if (_options.WorkdaysOnly)
        {
            while (candidate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                candidate = candidate.AddDays(1);
        }

        return candidate;
    }
}

public class BriefingSchedulerOptions
{
    public bool Enabled { get; set; } = true;
    public int ScheduleHourUtc { get; set; } = 7;
    public int ScheduleMinuteUtc { get; set; } = 0;
    public bool WorkdaysOnly { get; set; } = true;
}
