namespace EconomIA.Domain.Entities;

/// <summary>
/// Representa una earnings call (conference call de resultados) de una empresa.
/// Almacena el transcript completo y el resumen estructurado generado por IA.
/// </summary>
public class EarningsCall : Entity<Guid>
{
    public string CompanyName { get; private set; } = string.Empty;
    public string? Ticker { get; private set; }
    public Guid? CompanyId { get; private set; }
    public int FiscalYear { get; private set; }
    public int FiscalQuarter { get; private set; }
    public DateTime CallDate { get; private set; }
    public string? AudioFilePath { get; private set; }
    public string? TranscriptText { get; private set; }
    public string? Summary { get; private set; }
    public string? Guidance { get; private set; }
    public string? KeyMetrics { get; private set; }
    public string? Sentiment { get; private set; } // positive | neutral | negative
    public string Status { get; private set; } = "pending"; // pending | transcribing | analyzing | completed | failed
    public string? ErrorMessage { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public int? DurationSeconds { get; private set; }
    public string? Language { get; private set; }

    private EarningsCall() { }

    public static EarningsCall Create(
        string companyName,
        int fiscalYear,
        int fiscalQuarter,
        DateTime callDate,
        string? ticker = null,
        Guid? companyId = null)
    {
        if (string.IsNullOrWhiteSpace(companyName))
            throw new Exceptions.DomainException("Company name cannot be empty for earnings call.");
        if (fiscalQuarter < 1 || fiscalQuarter > 4)
            throw new Exceptions.DomainException("Fiscal quarter must be between 1 and 4.");
        if (fiscalYear < 2000 || fiscalYear > 2100)
            throw new Exceptions.DomainException("Fiscal year must be between 2000 and 2100.");

        return new EarningsCall
        {
            Id = Guid.NewGuid(),
            CompanyName = companyName,
            Ticker = ticker,
            CompanyId = companyId,
            FiscalYear = fiscalYear,
            FiscalQuarter = fiscalQuarter,
            CallDate = callDate,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    public void SetAudioFile(string filePath, int? durationSeconds = null)
    {
        AudioFilePath = filePath;
        DurationSeconds = durationSeconds;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkTranscribing()
    {
        Status = "transcribing";
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetTranscript(string transcript, string? language = null)
    {
        if (string.IsNullOrWhiteSpace(transcript))
            throw new Exceptions.DomainException("Transcript cannot be empty.");

        TranscriptText = transcript;
        Language = language;
        Status = "analyzing";
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAnalysis(string summary, string? guidance, string? keyMetrics, string? sentiment)
    {
        Summary = summary;
        Guidance = guidance;
        KeyMetrics = keyMetrics;
        Sentiment = sentiment;
        Status = "completed";
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkFailed(string error)
    {
        Status = "failed";
        ErrorMessage = error;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetTranscriptDirectly(string transcript, string? language = null)
    {
        TranscriptText = transcript;
        Language = language;
        Status = "analyzing";
        UpdatedAt = DateTime.UtcNow;
    }
}
