using EconomIA.Application.DTOs;
using EconomIA.Domain.Entities;
using EconomIA.Domain.Ports;
using Microsoft.AspNetCore.Mvc;

namespace EconomIA.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WatchlistsController : ControllerBase
{
    private readonly IWatchlistRepository _repository;

    public WatchlistsController(IWatchlistRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<WatchlistDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var watchlists = await _repository.GetAllAsync(ct);
        var dtos = watchlists.Select(MapToDto).ToList();
        return Ok(dtos);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(WatchlistDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var watchlist = await _repository.GetByIdAsync(id, ct);
        return watchlist is null ? NotFound() : Ok(MapToDto(watchlist));
    }

    [HttpPost]
    [ProducesResponseType(typeof(WatchlistDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateWatchlistRequest request, CancellationToken ct)
    {
        var watchlist = Watchlist.Create(request.Name, request.Description);

        await _repository.AddAsync(watchlist, ct);
        await _repository.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = watchlist.Id }, MapToDto(watchlist));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(WatchlistDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWatchlistRequest request, CancellationToken ct)
    {
        var watchlist = await _repository.GetByIdAsync(id, ct);
        if (watchlist is null) return NotFound();

        watchlist.Update(request.Name, request.Description);
        await _repository.UpdateAsync(watchlist, ct);
        await _repository.SaveChangesAsync(ct);

        return Ok(MapToDto(watchlist));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var watchlist = await _repository.GetByIdAsync(id, ct);
        if (watchlist is null) return NotFound();

        await _repository.DeleteAsync(id, ct);
        await _repository.SaveChangesAsync(ct);

        return NoContent();
    }

    [HttpPost("{id:guid}/items")]
    [ProducesResponseType(typeof(WatchlistItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddItem(Guid id, [FromBody] AddWatchlistItemRequest request, CancellationToken ct)
    {
        var watchlist = await _repository.GetByIdAsync(id, ct);
        if (watchlist is null) return NotFound();

        var item = watchlist.AddItem(request.EntityType, request.EntityId, request.Priority, request.PositionType, request.Thesis, request.Notes);
        await _repository.UpdateAsync(watchlist, ct);
        await _repository.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = watchlist.Id }, MapItemToDto(item));
    }

    [HttpDelete("{id:guid}/items/{itemId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveItem(Guid id, Guid itemId, CancellationToken ct)
    {
        var watchlist = await _repository.GetByIdAsync(id, ct);
        if (watchlist is null) return NotFound();

        watchlist.RemoveItem(itemId);
        await _repository.UpdateAsync(watchlist, ct);
        await _repository.SaveChangesAsync(ct);

        return NoContent();
    }

    private static WatchlistDto MapToDto(Watchlist w) => new(
        w.Id, w.Name, w.Description, w.CreatedAt, w.UpdatedAt,
        w.Items.Select(MapItemToDto).ToList());

    private static WatchlistItemDto MapItemToDto(WatchlistItem i) => new(
        i.Id, i.WatchlistId, i.EntityType, i.EntityId,
        i.Priority, i.PositionType, i.Thesis, i.Notes, i.CreatedAt);
}
