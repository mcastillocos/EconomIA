using EconomIA.Domain.Entities;
using EconomIA.Domain.Exceptions;

namespace EconomIA.Domain.Tests.Entities;

public class UploadedDocumentTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateDocument()
    {
        var doc = UploadedDocument.Create("company", Guid.NewGuid(), "report.pdf", "pdf", "/uploads/report.pdf", 1024 * 1024, "Annual Report");

        Assert.NotEqual(Guid.Empty, doc.Id);
        Assert.Equal("report.pdf", doc.FileName);
        Assert.Equal("pdf", doc.FileType);
        Assert.Equal("pending", doc.Status);
        Assert.Equal(1024 * 1024, doc.FileSize);
    }

    [Fact]
    public void Create_WithEmptyFileName_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() => UploadedDocument.Create("company", null, "", "pdf", "/path", 0));
    }

    [Fact]
    public void MarkProcessing_ShouldUpdateStatus()
    {
        var doc = UploadedDocument.Create("company", null, "test.csv", "csv", "/path", 100);

        doc.MarkProcessing();

        Assert.Equal("processing", doc.Status);
    }

    [Fact]
    public void MarkCompleted_ShouldUpdateStatusAndContent()
    {
        var doc = UploadedDocument.Create("company", null, "test.csv", "csv", "/path", 100);

        doc.MarkCompleted("extracted text", "Summary here", "{\"rows\": 10}");

        Assert.Equal("completed", doc.Status);
        Assert.Equal("extracted text", doc.ExtractedText);
        Assert.Equal("Summary here", doc.Summary);
    }

    [Fact]
    public void MarkFailed_ShouldUpdateStatusAndError()
    {
        var doc = UploadedDocument.Create("company", null, "test.csv", "csv", "/path", 100);

        doc.MarkFailed("Parse error at row 5");

        Assert.Equal("failed", doc.Status);
        Assert.Equal("Parse error at row 5", doc.ErrorMessage);
    }
}
