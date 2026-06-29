using EconomIA.Domain.Entities;
using EconomIA.Domain.Exceptions;

namespace EconomIA.Domain.Tests.Entities;

public class ChecklistInstanceTests
{
    private static Guid _templateId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_ShouldCreateInstance()
    {
        var instance = ChecklistInstance.Create(_templateId, "company", "Apple");

        Assert.NotEqual(Guid.Empty, instance.Id);
        Assert.Equal(_templateId, instance.TemplateId);
        Assert.Equal("company", instance.EntityType);
        Assert.Equal("Apple", instance.EntityName);
        Assert.Equal("in_progress", instance.Status);
        Assert.Null(instance.EntityId);
        Assert.Empty(instance.Answers);
    }

    [Fact]
    public void Create_WithEntityId_ShouldSetEntityId()
    {
        var entityId = Guid.NewGuid();
        var instance = ChecklistInstance.Create(_templateId, "company", "Apple", entityId);

        Assert.Equal(entityId, instance.EntityId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyEntityName_ShouldThrow(string? name)
    {
        Assert.Throws<DomainException>(() => ChecklistInstance.Create(_templateId, "company", name!));
    }

    [Fact]
    public void SetAnswer_NewItem_ShouldAddAnswer()
    {
        var instance = ChecklistInstance.Create(_templateId, "company", "Apple");
        var itemId = Guid.NewGuid();

        var answer = instance.SetAnswer(itemId, "true", "Sí, tiene foso");

        Assert.Single(instance.Answers);
        Assert.Equal(itemId, answer.TemplateItemId);
        Assert.Equal("true", answer.Value);
        Assert.Equal("Sí, tiene foso", answer.Comment);
        Assert.Equal(instance.Id, answer.InstanceId);
    }

    [Fact]
    public void SetAnswer_SameItem_ShouldReplace()
    {
        var instance = ChecklistInstance.Create(_templateId, "company", "Apple");
        var itemId = Guid.NewGuid();

        instance.SetAnswer(itemId, "true");
        instance.SetAnswer(itemId, "false", "Cambié de opinión");

        Assert.Single(instance.Answers);
        Assert.Equal("false", instance.Answers[0].Value);
        Assert.Equal("Cambié de opinión", instance.Answers[0].Comment);
    }

    [Fact]
    public void SetAnswer_MultipleItems_ShouldAccumulate()
    {
        var instance = ChecklistInstance.Create(_templateId, "company", "Apple");

        instance.SetAnswer(Guid.NewGuid(), "true");
        instance.SetAnswer(Guid.NewGuid(), "4");
        instance.SetAnswer(Guid.NewGuid(), "Buen negocio");

        Assert.Equal(3, instance.Answers.Count);
    }

    [Fact]
    public void MarkCompleted_ShouldChangeStatus()
    {
        var instance = ChecklistInstance.Create(_templateId, "company", "Apple");

        instance.MarkCompleted("Análisis terminado");

        Assert.Equal("completed", instance.Status);
        Assert.Equal("Análisis terminado", instance.Notes);
    }

    [Fact]
    public void Reopen_AfterCompleted_ShouldRevert()
    {
        var instance = ChecklistInstance.Create(_templateId, "company", "Apple");
        instance.MarkCompleted();

        instance.Reopen();

        Assert.Equal("in_progress", instance.Status);
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(5, 0, 0)]
    [InlineData(10, 5, 50)]
    [InlineData(10, 10, 100)]
    [InlineData(3, 1, 33)]
    public void CompletionPercentage_ShouldCalculateCorrectly(int totalItems, int answeredItems, int expectedPct)
    {
        var instance = ChecklistInstance.Create(_templateId, "company", "Apple");

        for (int i = 0; i < answeredItems; i++)
            instance.SetAnswer(Guid.NewGuid(), "true");

        Assert.Equal(expectedPct, instance.CompletionPercentage(totalItems));
    }

    [Fact]
    public void SetAnswer_ShouldUpdateTimestamp()
    {
        var instance = ChecklistInstance.Create(_templateId, "company", "Apple");
        var initial = instance.UpdatedAt;

        instance.SetAnswer(Guid.NewGuid(), "true");

        Assert.True(instance.UpdatedAt >= initial);
    }

    [Fact]
    public void MarkCompleted_WithoutNotes_ShouldLeaveNotesNull()
    {
        var instance = ChecklistInstance.Create(_templateId, "company", "Apple");

        instance.MarkCompleted();

        Assert.Equal("completed", instance.Status);
        Assert.Null(instance.Notes);
    }
}
