using EconomIA.Application.Interfaces;
using EconomIA.Infrastructure.ExternalServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EconomIA.Infrastructure.Tests.ExternalServices;

public class LlmServiceTests
{
    [Fact]
    public void Constructor_WithNoProviders_DoesNotThrow()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        var httpClient = new HttpClient();
        var logger = Mock.Of<ILogger<LlmService>>();

        // Act
        var service = new LlmService(httpClient, config, logger);

        // Assert — no exception
        Assert.NotNull(service);
    }

    [Fact]
    public async Task ChatAsync_WithNoProviders_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        var httpClient = new HttpClient();
        var logger = Mock.Of<ILogger<LlmService>>();
        var service = new LlmService(httpClient, config, logger);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ChatAsync("system", "user"));
    }

    [Fact]
    public void Constructor_WithAzureOpenAIConfig_BuildsProvider()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["LLM:AzureOpenAI:ApiKey"] = "test-key",
                ["LLM:AzureOpenAI:Endpoint"] = "https://test.openai.azure.com",
                ["LLM:AzureOpenAI:Deployment"] = "gpt-4o"
            })
            .Build();
        var httpClient = new HttpClient();
        var logger = Mock.Of<ILogger<LlmService>>();

        // Act — no throw means provider was built
        var service = new LlmService(httpClient, config, logger);
        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_WithClaudeConfig_BuildsProvider()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["LLM:Claude:ApiKey"] = "sk-test-key"
            })
            .Build();
        var httpClient = new HttpClient();
        var logger = Mock.Of<ILogger<LlmService>>();

        // Act
        var service = new LlmService(httpClient, config, logger);
        Assert.NotNull(service);
    }
}
