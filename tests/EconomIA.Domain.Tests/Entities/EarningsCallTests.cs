using EconomIA.Domain.Entities;
using EconomIA.Domain.Exceptions;

namespace EconomIA.Domain.Tests.Entities;

public class EarningsCallTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateEarningsCall()
    {
        var call = EarningsCall.Create("Apple", 2024, 3, new DateTime(2024, 8, 1), "AAPL");

        Assert.NotEqual(Guid.Empty, call.Id);
        Assert.Equal("Apple", call.CompanyName);
        Assert.Equal("AAPL", call.Ticker);
        Assert.Equal(2024, call.FiscalYear);
        Assert.Equal(3, call.FiscalQuarter);
        Assert.Equal("pending", call.Status);
        Assert.Null(call.TranscriptText);
        Assert.Null(call.Summary);
    }

    [Fact]
    public void Create_WithCompanyId_ShouldSetCompanyId()
    {
        var companyId = Guid.NewGuid();
        var call = EarningsCall.Create("Apple", 2024, 3, DateTime.UtcNow, "AAPL", companyId);

        Assert.Equal(companyId, call.CompanyId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyCompanyName_ShouldThrow(string? name)
    {
        Assert.Throws<DomainException>(() => EarningsCall.Create(name!, 2024, 1, DateTime.UtcNow));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(-1)]
    public void Create_WithInvalidQuarter_ShouldThrow(int quarter)
    {
        Assert.Throws<DomainException>(() => EarningsCall.Create("Apple", 2024, quarter, DateTime.UtcNow));
    }

    [Theory]
    [InlineData(1999)]
    [InlineData(2101)]
    public void Create_WithInvalidYear_ShouldThrow(int year)
    {
        Assert.Throws<DomainException>(() => EarningsCall.Create("Apple", year, 1, DateTime.UtcNow));
    }

    [Fact]
    public void SetAudioFile_ShouldSetPathAndDuration()
    {
        var call = EarningsCall.Create("Apple", 2024, 3, DateTime.UtcNow);

        call.SetAudioFile("/uploads/audio.mp3", 3600);

        Assert.Equal("/uploads/audio.mp3", call.AudioFilePath);
        Assert.Equal(3600, call.DurationSeconds);
    }

    [Fact]
    public void MarkTranscribing_ShouldChangeStatus()
    {
        var call = EarningsCall.Create("Apple", 2024, 3, DateTime.UtcNow);

        call.MarkTranscribing();

        Assert.Equal("transcribing", call.Status);
    }

    [Fact]
    public void SetTranscript_WithValidText_ShouldSetAndChangeStatus()
    {
        var call = EarningsCall.Create("Apple", 2024, 3, DateTime.UtcNow);

        call.SetTranscript("Good morning, welcome to Apple's Q3 earnings call...", "en");

        Assert.Equal("Good morning, welcome to Apple's Q3 earnings call...", call.TranscriptText);
        Assert.Equal("en", call.Language);
        Assert.Equal("analyzing", call.Status);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetTranscript_WithEmptyText_ShouldThrow(string? text)
    {
        var call = EarningsCall.Create("Apple", 2024, 3, DateTime.UtcNow);

        Assert.Throws<DomainException>(() => call.SetTranscript(text!));
    }

    [Fact]
    public void SetAnalysis_ShouldSetAllFieldsAndComplete()
    {
        var call = EarningsCall.Create("Apple", 2024, 3, DateTime.UtcNow);
        call.SetTranscript("Transcript text here");

        call.SetAnalysis(
            summary: "Revenue beat expectations",
            guidance: "Q4 expected $95B",
            keyMetrics: "Revenue: $94.8B, EPS: $1.40",
            sentiment: "positive"
        );

        Assert.Equal("completed", call.Status);
        Assert.Equal("Revenue beat expectations", call.Summary);
        Assert.Equal("Q4 expected $95B", call.Guidance);
        Assert.Equal("Revenue: $94.8B, EPS: $1.40", call.KeyMetrics);
        Assert.Equal("positive", call.Sentiment);
    }

    [Fact]
    public void MarkFailed_ShouldSetErrorAndStatus()
    {
        var call = EarningsCall.Create("Apple", 2024, 3, DateTime.UtcNow);

        call.MarkFailed("Transcription timeout");

        Assert.Equal("failed", call.Status);
        Assert.Equal("Transcription timeout", call.ErrorMessage);
    }

    [Fact]
    public void SetTranscriptDirectly_ShouldSetWithoutRequiringAudio()
    {
        var call = EarningsCall.Create("Microsoft", 2025, 1, DateTime.UtcNow, "MSFT");

        call.SetTranscriptDirectly("CEO Satya Nadella: Revenue was $62B...", "en");

        Assert.Equal("analyzing", call.Status);
        Assert.Contains("Satya Nadella", call.TranscriptText);
        Assert.Equal("en", call.Language);
        Assert.Null(call.AudioFilePath);
    }

    [Fact]
    public void FullWorkflow_Audio_ShouldTransitionCorrectly()
    {
        var call = EarningsCall.Create("Tesla", 2024, 4, new DateTime(2025, 1, 29), "TSLA");
        Assert.Equal("pending", call.Status);

        call.SetAudioFile("/uploads/tsla_q4_2024.mp3", 4500);
        call.MarkTranscribing();
        Assert.Equal("transcribing", call.Status);

        call.SetTranscript("Elon Musk: We delivered 1.8M vehicles in 2024...", "en");
        Assert.Equal("analyzing", call.Status);

        call.SetAnalysis("Tesla delivered 1.8M vehicles", "2025 target: 2.5M", "Deliveries: 1.8M", "positive");
        Assert.Equal("completed", call.Status);
    }
}
