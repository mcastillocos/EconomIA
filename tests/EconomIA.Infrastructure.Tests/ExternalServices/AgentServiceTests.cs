using EconomIA.Application.Interfaces;
using EconomIA.Domain.Entities;
using EconomIA.Infrastructure.ExternalServices;
using EconomIA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EconomIA.Infrastructure.Tests.ExternalServices;

public class AgentServiceTests
{
    private EconomIADbContext CreateInMemoryDb(string? dbName = null)
    {
        var name = dbName ?? Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<EconomIADbContext>()
            .UseInMemoryDatabase(databaseName: name)
            .Options;
        return new EconomIADbContext(options);
    }

    private IServiceProvider CreateServiceProvider(string dbName)
    {
        var services = new ServiceCollection();
        services.AddScoped<EconomIADbContext>(_ =>
        {
            var options = new DbContextOptionsBuilder<EconomIADbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new EconomIADbContext(options);
        });
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task RunAgentAsync_UnknownAgent_ReturnsFailed()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var sp = CreateServiceProvider(dbName);
        var llm = Mock.Of<ILlmService>();
        var logger = Mock.Of<ILogger<AgentService>>();
        var agents = Enumerable.Empty<IAgent>();

        var service = new AgentService(sp, llm, logger, agents);

        // Act
        var result = await service.RunAgentAsync("NonExistentAgent", "test input");

        // Assert
        Assert.Equal("failed", result.Status);
        Assert.Contains("not found", result.Error);
        Assert.NotEqual(Guid.Empty, result.RunId);
    }

    [Fact]
    public async Task RunAgentAsync_ValidAgent_ReturnsCompleted()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var sp = CreateServiceProvider(dbName);
        var llm = Mock.Of<ILlmService>();
        var logger = Mock.Of<ILogger<AgentService>>();

        var mockAgent = new Mock<IAgent>();
        mockAgent.Setup(a => a.Name).Returns("TestAgent");
        mockAgent.Setup(a => a.ExecuteAsync(It.IsAny<ILlmService>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentOutput { Output = "Analysis result", Sources = "TestSource" });

        var agents = new[] { mockAgent.Object };
        var service = new AgentService(sp, llm, logger, agents);

        // Act
        var result = await service.RunAgentAsync("TestAgent", "test input");

        // Assert
        Assert.Equal("completed", result.Status);
        Assert.Equal("Analysis result", result.Output);
        Assert.Equal("TestSource", result.Sources);
        Assert.Equal("TestAgent", result.AgentName);
    }

    [Fact]
    public async Task RunAgentAsync_AgentThrows_ReturnsFailedWithError()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var sp = CreateServiceProvider(dbName);
        var llm = Mock.Of<ILlmService>();
        var logger = Mock.Of<ILogger<AgentService>>();

        var mockAgent = new Mock<IAgent>();
        mockAgent.Setup(a => a.Name).Returns("FailAgent");
        mockAgent.Setup(a => a.ExecuteAsync(It.IsAny<ILlmService>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("LLM timeout"));

        var agents = new[] { mockAgent.Object };
        var service = new AgentService(sp, llm, logger, agents);

        // Act
        var result = await service.RunAgentAsync("FailAgent", "test");

        // Assert
        Assert.Equal("failed", result.Status);
        Assert.Contains("LLM timeout", result.Error);
    }

    [Fact]
    public async Task RunAgentAsync_PersistsAgentRun()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var sp = CreateServiceProvider(dbName);
        var llm = Mock.Of<ILlmService>();
        var logger = Mock.Of<ILogger<AgentService>>();

        var mockAgent = new Mock<IAgent>();
        mockAgent.Setup(a => a.Name).Returns("PersistAgent");
        mockAgent.Setup(a => a.ExecuteAsync(It.IsAny<ILlmService>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentOutput { Output = "ok" });

        var agents = new[] { mockAgent.Object };
        var service = new AgentService(sp, llm, logger, agents);

        // Act
        var result = await service.RunAgentAsync("PersistAgent", "input data");

        // Assert — use fresh context to verify persistence
        using var verifyDb = CreateInMemoryDb(dbName);
        var saved = await verifyDb.AgentRuns.FirstOrDefaultAsync(r => r.Id == result.RunId);
        Assert.NotNull(saved);
        Assert.Equal("completed", saved.Status);
        Assert.Equal("PersistAgent", saved.AgentName);
        Assert.Equal("input data", saved.Input);
        Assert.Equal("ok", saved.Output);
    }
}
