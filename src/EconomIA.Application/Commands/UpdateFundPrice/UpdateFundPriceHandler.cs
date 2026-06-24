using EconomIA.Domain.Ports;
using EconomIA.Domain.ValueObjects;
using EconomIA.Application.Interfaces;
using MediatR;

namespace EconomIA.Application.Commands.UpdateFundPrice;

public class UpdateFundPriceHandler : IRequestHandler<UpdateFundPriceCommand, bool>
{
    private readonly IFundRepository _fundRepository;
    private readonly ICacheService _cacheService;
    private readonly IEventBus _eventBus;
    private readonly IFundNotificationService _notificationService;

    public UpdateFundPriceHandler(
        IFundRepository fundRepository,
        ICacheService cacheService,
        IEventBus eventBus,
        IFundNotificationService notificationService)
    {
        _fundRepository = fundRepository;
        _cacheService = cacheService;
        _eventBus = eventBus;
        _notificationService = notificationService;
    }

    public async Task<bool> Handle(UpdateFundPriceCommand request, CancellationToken cancellationToken)
    {
        var fund = await _fundRepository.GetByIdAsync(request.FundId, cancellationToken);
        if (fund is null) return false;

        var newPrice = new Money(request.NewPrice, request.Currency);
        fund.UpdatePrice(newPrice);

        await _fundRepository.UpdateAsync(fund, cancellationToken);
        await _fundRepository.SaveChangesAsync(cancellationToken);

        // Invalidate cache
        await _cacheService.RemoveAsync($"fund:{fund.Id}", cancellationToken);
        await _cacheService.RemoveAsync("top-funds:100", cancellationToken);

        // Publish domain events
        foreach (var domainEvent in fund.DomainEvents)
        {
            await _eventBus.PublishAsync(domainEvent, cancellationToken);
        }
        fund.ClearDomainEvents();

        // Notify connected clients via SignalR
        await _notificationService.NotifyPriceUpdateAsync(
            fund.Id, fund.Name, request.NewPrice, request.Currency, cancellationToken);

        return true;
    }
}
