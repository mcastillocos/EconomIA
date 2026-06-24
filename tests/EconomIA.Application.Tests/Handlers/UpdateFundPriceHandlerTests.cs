using EconomIA.Application.Commands.UpdateFundPrice;
using EconomIA.Application.Interfaces;
using EconomIA.Domain.Entities;
using EconomIA.Domain.Ports;
using EconomIA.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace EconomIA.Application.Tests.Handlers;

public class UpdateFundPriceHandlerTests
{
    private readonly IFundRepository _fundRepository = Substitute.For<IFundRepository>();
    private readonly ICacheService _cacheService = Substitute.For<ICacheService>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();
    private readonly IFundNotificationService _notificationService = Substitute.For<IFundNotificationService>();
    private readonly UpdateFundPriceHandler _handler;

    public UpdateFundPriceHandlerTests()
    {
        _handler = new UpdateFundPriceHandler(
            _fundRepository, _cacheService, _eventBus, _notificationService);
    }

    [Fact]
    public async Task Handle_FundExists_ShouldUpdatePriceAndNotify()
    {
        // Arrange
        var fund = Fund.Create(
            new ISIN("ES0000000001"), "Test Fund", "Category", "Company",
            RiskLevel.Medium, new Money(100m, "EUR"), new Percentage(1.5m));

        _fundRepository.GetByIdAsync(fund.Id, Arg.Any<CancellationToken>())
            .Returns(fund);

        var command = new UpdateFundPriceCommand(fund.Id, 150m, "EUR");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        fund.NetAssetValue.Amount.Should().Be(150m);

        await _fundRepository.Received(1).UpdateAsync(fund, Arg.Any<CancellationToken>());
        await _fundRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _cacheService.Received().RemoveAsync($"fund:{fund.Id}", Arg.Any<CancellationToken>());
        await _notificationService.Received(1).NotifyPriceUpdateAsync(
            fund.Id, fund.Name, 150m, "EUR", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_FundNotFound_ShouldReturnFalse()
    {
        // Arrange
        _fundRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Fund?)null);

        var command = new UpdateFundPriceCommand(Guid.NewGuid(), 150m, "EUR");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        await _fundRepository.DidNotReceive().UpdateAsync(Arg.Any<Fund>(), Arg.Any<CancellationToken>());
    }
}
