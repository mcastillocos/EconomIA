namespace EconomIA.Application.DTOs;

public record WatchlistDto(
    Guid Id,
    string Name,
    string? Description,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<WatchlistItemDto> Items);

public record WatchlistItemDto(
    Guid Id,
    Guid WatchlistId,
    string EntityType,
    Guid EntityId,
    int Priority,
    string PositionType,
    string? Thesis,
    string? Notes,
    DateTime CreatedAt);

public record CreateWatchlistRequest(
    string Name,
    string? Description);

public record UpdateWatchlistRequest(
    string Name,
    string? Description);

public record AddWatchlistItemRequest(
    string EntityType,
    Guid EntityId,
    int Priority = 0,
    string PositionType = "watch",
    string? Thesis = null,
    string? Notes = null);
