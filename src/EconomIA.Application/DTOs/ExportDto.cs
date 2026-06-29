using EconomIA.Application.Interfaces;

namespace EconomIA.Application.DTOs;

public record ExportRequest(
    string Type, // "funds", "portfolio", "report", "earnings-call"
    string Format, // "pdf", "excel"
    string? EntityId = null,
    Dictionary<string, string>? Filters = null
);

public record ExportResult(
    byte[] FileContent,
    string ContentType,
    string FileName
);
