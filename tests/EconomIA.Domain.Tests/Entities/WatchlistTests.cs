using EconomIA.Domain.Entities;
using EconomIA.Domain.Exceptions;

namespace EconomIA.Domain.Tests.Entities;

public class WatchlistTests
{
    [Fact]
    public void Create_WithValidName_ShouldCreateWatchlist()
    {
        var watchlist = Watchlist.Create("Mi Cartera", "Posiciones activas");

        Assert.NotEqual(Guid.Empty, watchlist.Id);
        Assert.Equal("Mi Cartera", watchlist.Name);
        Assert.Equal("Posiciones activas", watchlist.Description);
        Assert.Empty(watchlist.Items);
    }

    [Fact]
    public void Create_WithEmptyName_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() => Watchlist.Create(""));
    }

    [Fact]
    public void AddItem_ShouldAddToItems()
    {
        var watchlist = Watchlist.Create("Test");
        var entityId = Guid.NewGuid();

        var item = watchlist.AddItem("company", entityId, 1, "real", "Tesis de inversión", "Notas");

        Assert.Single(watchlist.Items);
        Assert.Equal("company", item.EntityType);
        Assert.Equal(entityId, item.EntityId);
        Assert.Equal(1, item.Priority);
        Assert.Equal("real", item.PositionType);
        Assert.Equal("Tesis de inversión", item.Thesis);
    }

    [Fact]
    public void RemoveItem_ShouldRemoveFromItems()
    {
        var watchlist = Watchlist.Create("Test");
        var item = watchlist.AddItem("fund", Guid.NewGuid());

        watchlist.RemoveItem(item.Id);

        Assert.Empty(watchlist.Items);
    }

    [Fact]
    public void RemoveItem_WithNonExistentId_ShouldNotThrow()
    {
        var watchlist = Watchlist.Create("Test");
        watchlist.AddItem("fund", Guid.NewGuid());

        watchlist.RemoveItem(Guid.NewGuid()); // Non-existent

        Assert.Single(watchlist.Items);
    }

    [Fact]
    public void Update_ShouldUpdateNameAndDescription()
    {
        var watchlist = Watchlist.Create("Original", "Desc original");

        watchlist.Update("Nuevo nombre", "Nueva desc");

        Assert.Equal("Nuevo nombre", watchlist.Name);
        Assert.Equal("Nueva desc", watchlist.Description);
    }

    [Fact]
    public void Update_WithEmptyName_ShouldThrowDomainException()
    {
        var watchlist = Watchlist.Create("Test");
        Assert.Throws<DomainException>(() => watchlist.Update("", null));
    }
}
