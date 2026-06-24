using EconomIA.Application.Commands.UpdateFundPrice;
using EconomIA.Application.Behaviors;
using FluentAssertions;
using FluentValidation;
using MediatR;
using NSubstitute;

namespace EconomIA.Application.Tests.Behaviors;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_ValidRequest_ShouldCallNext()
    {
        // Arrange
        var validators = new List<IValidator<UpdateFundPriceCommand>>
        {
            new UpdateFundPriceValidator()
        };

        var behavior = new ValidationBehavior<UpdateFundPriceCommand, bool>(validators);
        var command = new UpdateFundPriceCommand(Guid.NewGuid(), 100m, "EUR");

        // Act & Assert
        var result = await behavior.Handle(command, (ct) => Task.FromResult(true), CancellationToken.None);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_InvalidRequest_ShouldThrowValidationException()
    {
        // Arrange
        var validators = new List<IValidator<UpdateFundPriceCommand>>
        {
            new UpdateFundPriceValidator()
        };

        var behavior = new ValidationBehavior<UpdateFundPriceCommand, bool>(validators);
        var command = new UpdateFundPriceCommand(Guid.Empty, -1m, ""); // Invalid

        // Act
        var act = () => behavior.Handle(command, (ct) => Task.FromResult(true), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }
}
