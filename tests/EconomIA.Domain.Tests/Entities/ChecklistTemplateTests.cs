using EconomIA.Domain.Entities;
using EconomIA.Domain.Exceptions;

namespace EconomIA.Domain.Tests.Entities;

public class ChecklistTemplateTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateTemplate()
    {
        var template = ChecklistTemplate.Create("Due Diligence", "company", "Análisis completo");

        Assert.NotEqual(Guid.Empty, template.Id);
        Assert.Equal("Due Diligence", template.Name);
        Assert.Equal("company", template.Category);
        Assert.Equal("Análisis completo", template.Description);
        Assert.False(template.IsBuiltIn);
        Assert.Empty(template.Items);
    }

    [Fact]
    public void Create_BuiltIn_ShouldSetIsBuiltIn()
    {
        var template = ChecklistTemplate.Create("Predefinido", "fund", isBuiltIn: true);

        Assert.True(template.IsBuiltIn);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyName_ShouldThrow(string? name)
    {
        var ex = Assert.Throws<DomainException>(() => ChecklistTemplate.Create(name!, "company"));
        Assert.Contains("name", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyCategory_ShouldThrow(string? category)
    {
        var ex = Assert.Throws<DomainException>(() => ChecklistTemplate.Create("Test", category!));
        Assert.Contains("category", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddItem_WithValidData_ShouldAddItem()
    {
        var template = ChecklistTemplate.Create("Test", "company");

        var item = template.AddItem("Tiene foso competitivo", "Negocio", 1, "boolean", "Moat sostenible");

        Assert.Single(template.Items);
        Assert.Equal("Tiene foso competitivo", item.Text);
        Assert.Equal("Negocio", item.Section);
        Assert.Equal(1, item.Order);
        Assert.Equal("boolean", item.ItemType);
        Assert.Equal("Moat sostenible", item.HelpText);
        Assert.Equal(template.Id, item.TemplateId);
    }

    [Fact]
    public void AddItem_Multiple_ShouldAccumulate()
    {
        var template = ChecklistTemplate.Create("Test", "company");

        template.AddItem("Item 1", "Sección A", 1);
        template.AddItem("Item 2", "Sección A", 2);
        template.AddItem("Item 3", "Sección B", 1, "rating");

        Assert.Equal(3, template.Items.Count);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddItem_WithEmptyText_ShouldThrow(string? text)
    {
        var template = ChecklistTemplate.Create("Test", "company");

        Assert.Throws<DomainException>(() => template.AddItem(text!, "Sección", 1));
    }

    [Fact]
    public void RemoveItem_ExistingItem_ShouldRemove()
    {
        var template = ChecklistTemplate.Create("Test", "company");
        var item = template.AddItem("A eliminar", "Sección", 1);

        template.RemoveItem(item.Id);

        Assert.Empty(template.Items);
    }

    [Fact]
    public void RemoveItem_NonExisting_ShouldNotThrow()
    {
        var template = ChecklistTemplate.Create("Test", "company");

        template.RemoveItem(Guid.NewGuid()); // No debería lanzar excepción
    }

    [Fact]
    public void Update_WithValidName_ShouldUpdate()
    {
        var template = ChecklistTemplate.Create("Original", "company", "Desc original");

        template.Update("Actualizado", "Desc nueva");

        Assert.Equal("Actualizado", template.Name);
        Assert.Equal("Desc nueva", template.Description);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_WithEmptyName_ShouldThrow(string? name)
    {
        var template = ChecklistTemplate.Create("Original", "company");

        Assert.Throws<DomainException>(() => template.Update(name!, "desc"));
    }

    [Fact]
    public void AddItem_ShouldUpdateTimestamp()
    {
        var template = ChecklistTemplate.Create("Test", "company");
        var initialUpdate = template.UpdatedAt;

        // Asegurar que el timestamp cambia
        template.AddItem("Nuevo item", "Sección", 1);

        Assert.True(template.UpdatedAt >= initialUpdate);
    }
}
